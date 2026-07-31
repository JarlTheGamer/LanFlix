using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.FFmpeg;

/// <summary>
/// Enhanced transcoding pipeline with modern codec support and hardware acceleration
/// </summary>
public class EnhancedTranscodingPipeline : ITranscodingPipeline
{
    private readonly ILogger<EnhancedTranscodingPipeline> _logger;
    private readonly string _ffmpegPath;
    private readonly TranscodingSettings _settings;

    public EnhancedTranscodingPipeline(
        ILogger<EnhancedTranscodingPipeline> logger,
        TranscodingSettings settings)
    {
        _logger = logger;
        _settings = settings;
        _ffmpegPath = FindFFmpegPath();
    }

    public async IAsyncEnumerable<ReadOnlyMemory<byte>> StreamAsync(
        TranscodeRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting transcoding pipeline for session {SessionId}: {Mode}",
            request.SessionId, request.Mode);

        var arguments = BuildFFmpegArguments(request);
        
        _logger.LogInformation("FFmpeg command: {FFmpegPath} {Arguments}", _ffmpegPath, arguments);

        Process? process = null;
        
        try
        {
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var buffer = new byte[64 * 1024]; // 64KB buffer
            var outputStream = process.StandardOutput.BaseStream;
            var startTime = DateTime.UtcNow;
            var lastLogTime = startTime;
            var totalBytesRead = 0L;

            // Start reading stderr in background to capture FFmpeg errors
            var errorOutput = new StringBuilder();
            var errorTask = Task.Run(async () =>
            {
                try
                {
                    var errorReader = process.StandardError;
                    var errorBuffer = new char[1024];
                    int charsRead;
                    while ((charsRead = await errorReader.ReadAsync(errorBuffer, 0, errorBuffer.Length)) > 0)
                    {
                        errorOutput.Append(errorBuffer, 0, charsRead);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading FFmpeg stderr for session {SessionId}", request.SessionId);
                }
            });

            while (!process.HasExited && !cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await outputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                
                if (bytesRead == 0)
                {
                    // Check if process is still running
                    if (process.HasExited)
                        break;
                    
                    // Log progress every 5 seconds if no data
                    var now = DateTime.UtcNow;
                    if ((now - lastLogTime).TotalSeconds >= 5)
                    {
                        _logger.LogWarning("FFmpeg session {SessionId} - No output for {Seconds}s, process running: {IsRunning}, total bytes: {Bytes}", 
                            request.SessionId, (now - startTime).TotalSeconds, !process.HasExited, totalBytesRead);
                        
                        // Log any error output we've captured
                        var currentError = errorOutput.ToString();
                        if (!string.IsNullOrEmpty(currentError))
                        {
                            _logger.LogError("FFmpeg stderr for session {SessionId}: {Error}", request.SessionId, currentError);
                        }
                        
                        lastLogTime = now;
                        
                        // If no output for 30 seconds, something is wrong
                        if ((now - startTime).TotalSeconds >= 30 && totalBytesRead == 0)
                        {
                            _logger.LogError("FFmpeg session {SessionId} - No output after 30 seconds, terminating process", request.SessionId);
                            throw new TimeoutException("FFmpeg process appears to be hanging - no output after 30 seconds");
                        }
                    }
                    
                    // Small delay to prevent busy waiting
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                totalBytesRead += bytesRead;
                
                // Log first successful output
                if (totalBytesRead == bytesRead)
                {
                    _logger.LogInformation("FFmpeg session {SessionId} - First output received: {Bytes} bytes after {Seconds}s", 
                        request.SessionId, bytesRead, (DateTime.UtcNow - startTime).TotalSeconds);
                }

                yield return new ReadOnlyMemory<byte>(buffer, 0, bytesRead);
            }

            // Wait for process to complete
            await process.WaitForExitAsync(cancellationToken);

            // Get final error output
            await errorTask;
            var finalError = errorOutput.ToString();

            if (process.ExitCode != 0)
            {
                _logger.LogError("FFmpeg process failed with exit code {ExitCode}: {Error}",
                    process.ExitCode, finalError);
                throw new InvalidOperationException($"Transcoding failed: {finalError}");
            }

            _logger.LogInformation("Transcoding completed successfully for session {SessionId}, total bytes: {Bytes}", 
                request.SessionId, totalBytesRead);
        }
        finally
        {
            if (process != null)
            {
                if (!process.HasExited)
                {
                    try
                    {
                        // Try graceful termination first
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Transcoding completed, terminating FFmpeg process for session {SessionId}", request.SessionId);
                        }
                        else
                        {
                            _logger.LogInformation("Transcoding cancelled, terminating FFmpeg process for session {SessionId}", request.SessionId);
                        }
                        
                        process.Kill();
                    }
                    catch (Exception killEx)
                    {
                        _logger.LogWarning(killEx, "Failed to terminate FFmpeg process for session {SessionId}", request.SessionId);
                    }
                }
                
                process.Dispose();
            }
        }
    }

    public async Task TranscodeToFileAsync(
        TranscodeRequest request,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting offline transcoding for session {SessionId}: {Input} -> {Output}",
            request.SessionId, request.InputPath, outputPath);

        var arguments = BuildFFmpegArguments(request, outputPath);
        
        _logger.LogInformation("FFmpeg offline command: {FFmpegPath} {Arguments}", _ffmpegPath, arguments);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        var errorOutput = new StringBuilder();
        process.ErrorDataReceived += (s, e) => 
        {
            if (e.Data != null)
            {
                errorOutput.AppendLine(e.Data);
                // Log progress periodically or on specific markers if needed
                if (e.Data.Contains("frame="))
                {
                    _logger.LogDebug("FFmpeg Progress [{SessionId}]: {Data}", request.SessionId, e.Data);
                }
            }
        };

        process.Start();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = errorOutput.ToString();
                _logger.LogError("Offline transcoding failed with exit code {ExitCode}: {Error}",
                    process.ExitCode, error);
                throw new InvalidOperationException($"Offline transcoding failed: {error}");
            }

            _logger.LogInformation("Offline transcoding completed successfully for session {SessionId}", request.SessionId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Offline transcoding cancelled for session {SessionId}", request.SessionId);
            if (!process.HasExited)
            {
                process.Kill(true);
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during offline transcoding for session {SessionId}", request.SessionId);
            if (!process.HasExited)
            {
                process.Kill(true);
            }
            throw;
        }
    }

    private string BuildFFmpegArguments(TranscodeRequest request, string? outputPath = null)
    {
        var args = new StringBuilder();
        
        // Add global overwriting flag for offline transcodes
        if (!string.IsNullOrEmpty(outputPath))
        {
            args.Append("-y "); // Overwrite output files
        }

        // Hardware acceleration setup (must be before input)
        if (request.HwAccelMethod != HwAccelMethod.None)
        {
            AddHardwareAccelerationArgs(args, request.HwAccelMethod);
        }

        // Input seeking
        if (request.StartPosition.HasValue && request.StartPosition.Value > 0)
        {
            args.Append($"-ss {request.StartPosition.Value:F3} ");
            // Add noaccurate_seek for faster seeking
            args.Append("-noaccurate_seek ");
            // Add additional seeking optimizations
            args.Append("-seek2any 1 ");
        }
        
        args.Append($"-i \"{request.InputPath}\" ");

        // Video encoding
        AddVideoEncodingArgs(args, request);

        // Audio encoding
        AddAudioEncodingArgs(args, request);

        // Subtitle handling - Removed to prevent burning and improve performance
        // Subtitles are now handled externally by the frontend
        /*
        if (request.SubtitleStreamIndex.HasValue)
        {
            args.Append($"-map 0:s:{request.SubtitleStreamIndex.Value} ");
        }
        */

        // Output format and container
        AddOutputFormatArgs(args, request);

        // Threading
        if (_settings.ThreadCount > 0)
        {
            args.Append($"-threads {_settings.ThreadCount} ");
        }

        // General options for better seeking
        args.Append("-avoid_negative_ts make_zero ");
        args.Append("-fflags +genpts ");
        
        // Add keyframe settings for better seeking
        if (request.Mode != StreamingMode.DirectPlay && _settings.EnableSeekingOptimizations)
        {
            var keyframeInterval = _settings.SeekingKeyframeInterval;
            args.Append($"-g {keyframeInterval} "); // Keyframe every N frames for better seeking
            args.Append($"-keyint_min {keyframeInterval} "); // Minimum keyframe interval
            args.Append("-sc_threshold 0 "); // Disable scene change detection to maintain regular keyframes
            
            // Additional seeking optimizations
            if (request.StartPosition.HasValue && request.StartPosition.Value > 0)
            {
                // Force keyframe at start for better seeking accuracy
                args.Append("-force_key_frames 0 ");
            }
        }
        
        // Determine output format - use MPEG-TS for better seeking
        var outputFormat = GetOutputFormat(request.OutputFormat);
        
        // Add format-specific options for streaming
        if (outputFormat == "mp4")
        {
            // MP4 streaming options optimized for duration metadata
            // Use default_base_moof instead of empty_moov to include duration info
            args.Append("-movflags frag_keyframe+default_base_moof+faststart ");
            
            // Add fragment duration settings for better streaming
            args.Append("-min_frag_duration 1000000 "); // 1 second fragments
            args.Append("-frag_duration 2000000 ");     // 2 second max fragments
        }
        else if (outputFormat == "mpegts")
        {
            // MPEG-TS options for better seeking support
            args.Append("-mpegts_m2ts_mode 0 ");
            args.Append("-mpegts_copyts 1 ");
        }
        
        args.Append($"-f {outputFormat} ");

        // Output destination
        if (string.IsNullOrEmpty(outputPath))
        {
            // Output to stdout for streaming
            args.Append("pipe:1");
        }
        else
        {
            // Output to file for offline transcoding
            args.Append($"\"{outputPath}\"");
        }

        return args.ToString();
    }

    private void AddHardwareAccelerationArgs(StringBuilder args, HwAccelMethod method)
    {
        // Only add hardware acceleration if user has it enabled
        if (!_settings.EnableHardwareAcceleration)
        {
            _logger.LogInformation("Hardware acceleration disabled in settings");
            return;
        }

        if (method == HwAccelMethod.None)
        {
            _logger.LogInformation("No hardware acceleration method available");
            return;
        }

        _logger.LogInformation("Adding hardware acceleration: {Method}", method);

        switch (method)
        {
            case HwAccelMethod.Nvenc:
                args.Append("-hwaccel cuda ");
                // Skip cuda output format for simpler pipeline
                // args.Append("-hwaccel_output_format cuda ");
                if (_settings.EnableLowPowerEncoding)
                {
                    args.Append("-gpu 0 ");
                }
                break;
            case HwAccelMethod.QuickSync:
                args.Append("-hwaccel qsv ");
                args.Append("-hwaccel_output_format qsv ");
                break;
            case HwAccelMethod.Amf:
                args.Append("-hwaccel d3d11va ");
                break;
            case HwAccelMethod.Vaapi:
                args.Append("-hwaccel vaapi ");
                args.Append("-hwaccel_device /dev/dri/renderD128 ");
                args.Append("-hwaccel_output_format vaapi ");
                break;
            case HwAccelMethod.VideoToolbox:
                args.Append("-hwaccel videotoolbox ");
                break;
            case HwAccelMethod.Rockchip:
                args.Append("-hwaccel rkmpp ");
                break;
        }
    }

    private void AddVideoEncodingArgs(StringBuilder args, TranscodeRequest request)
    {
        if (request.TargetVideoCodec == "copy")
        {
            args.Append("-c:v copy ");
            return;
        }

        // Video codec - use hardware-accelerated version if available and enabled
        var videoCodec = GetOptimalVideoCodec(request.TargetVideoCodec, request.HwAccelMethod);
        args.Append($"-c:v {videoCodec} ");

        // Video bitrate - optimized for high quality
        if (request.TargetVideoBitrate.HasValue)
        {
            var bitrate = request.TargetVideoBitrate.Value;
            var targetCodec = GetOptimalVideoCodec(request.TargetVideoCodec, request.HwAccelMethod);
            
            // For hardware encoding, use high-quality bitrate control
            if (targetCodec.Contains("nvenc"))
            {
                args.Append($"-b:v {bitrate} ");
                args.Append($"-maxrate {bitrate * 1.1:F0} "); // Smaller maxrate buffer for consistent quality
                args.Append($"-bufsize {bitrate} "); // Larger buffer for quality consistency
                args.Append("-rc vbr_hq "); // High quality VBR mode for NVENC
                args.Append("-spatial_aq 1 "); // Spatial adaptive quantization for better quality
                args.Append("-temporal_aq 1 "); // Temporal adaptive quantization
            }
            else if (targetCodec.Contains("qsv"))
            {
                args.Append($"-b:v {bitrate} ");
                args.Append($"-maxrate {bitrate * 1.1:F0} ");
                args.Append($"-bufsize {bitrate} ");
                args.Append("-look_ahead 1 "); // Enable look-ahead for better quality
            }
            else if (targetCodec.Contains("amf"))
            {
                args.Append($"-b:v {bitrate} ");
                args.Append($"-maxrate {bitrate * 1.1:F0} ");
                args.Append($"-bufsize {bitrate} ");
                args.Append("-rc vbr_peak "); // Variable bitrate peak mode for quality
            }
            else
            {
                // Software encoding - use higher quality settings
                args.Append($"-b:v {bitrate} ");
                args.Append($"-maxrate {bitrate * 1.05:F0} "); // Tighter maxrate for consistent quality
                args.Append($"-bufsize {bitrate * 1.5:F0} "); // Larger buffer for quality
            }
        }

        // Build video filter chain
        var videoFilters = new List<string>();

        // Resolution scaling
        if (request.TargetWidth.HasValue && request.TargetHeight.HasValue)
        {
            if (request.HwAccelMethod == HwAccelMethod.Vaapi)
            {
                videoFilters.Add($"scale_vaapi={request.TargetWidth.Value}:{request.TargetHeight.Value}");
            }
            else if (request.HwAccelMethod == HwAccelMethod.Nvenc)
            {
                // For NVENC, use regular scale if we're doing tone mapping (to avoid GPU/CPU conflicts)
                if (request.SourceMedia.Video.IsHDR && _settings.EnableToneMapping)
                {
                    videoFilters.Add($"scale={request.TargetWidth.Value}:{request.TargetHeight.Value}");
                }
                else
                {
                    videoFilters.Add($"scale_cuda={request.TargetWidth.Value}:{request.TargetHeight.Value}");
                }
            }
            else
            {
                videoFilters.Add($"scale={request.TargetWidth.Value}:{request.TargetHeight.Value}");
            }
        }

        // HDR tone mapping - simplified for speed
        if (request.SourceMedia.Video.IsHDR && _settings.EnableToneMapping)
        {
            // For hardware acceleration, use simpler tone mapping or skip it for speed
            if (request.HwAccelMethod == HwAccelMethod.Nvenc)
            {
                // Skip complex tone mapping for NVENC - let the display handle HDR
                _logger.LogInformation("Skipping tone mapping for NVENC hardware acceleration (speed optimization)");
            }
            else
            {
                var toneMappingFilters = GetToneMappingFilters(request.HwAccelMethod);
                videoFilters.AddRange(toneMappingFilters);
            }
        }

        // Apply video filters if any
        if (videoFilters.Count > 0)
        {
            args.Append($"-vf \"{string.Join(",", videoFilters)}\" ");
        }

        // Set pixel format for optimal quality
        var codec = GetOptimalVideoCodec(request.TargetVideoCodec, request.HwAccelMethod);
        if (codec.Contains("nvenc"))
        {
            args.Append("-pix_fmt yuv420p "); // Standard pixel format for NVENC
        }
        else if (codec.Contains("x264") || codec.Contains("x265"))
        {
            args.Append("-pix_fmt yuv420p "); // Standard pixel format for software encoding
        }

        // Encoding preset
        if (!string.IsNullOrEmpty(videoCodec))
        {
            AddEncodingPreset(args, videoCodec);
        }

        // High-quality settings for all encoders
        // Note: codec variable already declared above for pixel format
        if (codec.Contains("nvenc"))
        {
            // High-quality NVENC settings
            if (!request.TargetVideoBitrate.HasValue)
            {
                // Use CQ mode for quality when no bitrate specified
                var cqValue = _settings.TargetQuality ?? 23; // Use standard high quality (23) as default
                args.Append($"-cq {cqValue} "); // Use the setting directly to avoid over-inflating size
            }
            
            if (_settings.EnableBFrames)
            {
                args.Append("-bf 3 "); // More B-frames for better compression
            }
            args.Append("-refs 4 "); // More reference frames for quality
        }
        else if (codec.Contains("qsv"))
        {
            // High-quality QuickSync settings
            if (!request.TargetVideoBitrate.HasValue && _settings.TargetQuality.HasValue)
            {
                args.Append($"-global_quality {_settings.TargetQuality.Value + 3} "); // QSV quality scale
            }
            if (_settings.EnableBFrames)
            {
                args.Append("-bf 3 ");
            }
        }
        else if (codec.Contains("amf"))
        {
            // High-quality AMF settings
            if (_settings.EnableBFrames)
            {
                args.Append("-bf 3 ");
            }
        }
        else
        {
            // Software encoding with CRF for highest quality
            if (codec.Contains("x264") || codec.Contains("x265"))
            {
                var crfValue = _settings.TargetQuality ?? 23; // Use standard high quality (23) as default
                args.Append($"-crf {crfValue} ");
                // Additional quality settings for software encoding
                if (codec.Contains("x264"))
                {
                    args.Append("-profile:v high "); // High profile for better quality
                    args.Append("-level 4.1 "); // Higher level for more features
                    args.Append("-me_method umh "); // Better motion estimation
                    args.Append("-subme 8 "); // Higher subpixel motion estimation
                    args.Append("-trellis 2 "); // Trellis quantization for better quality
                    args.Append("-mixed-refs 1 "); // Mixed references
                }
                else if (codec.Contains("x265"))
                {
                    args.Append("-profile:v main "); // Main profile for HEVC
                    args.Append("-tier main "); // Main tier
                    args.Append("-me 3 "); // Star motion estimation (highest quality)
                    args.Append("-subme 4 "); // Higher subpixel refinement
                    args.Append("-rd 4 "); // Rate-distortion optimization level
                }
            }
        }
    }

    private void AddAudioEncodingArgs(StringBuilder args, TranscodeRequest request)
    {
        if (request.TargetAudioCodec == "copy")
        {
            args.Append("-c:a copy ");
            return;
        }

        // Audio codec
        args.Append($"-c:a {request.TargetAudioCodec} ");

        // Downmix multi-channel audio to stereo AAC for 100% browser compatibility and to prevent FFmpeg 5.1 channel layout error -22
        if (request.TargetAudioCodec == "aac")
        {
            args.Append("-ac 2 ");
        }

        // Audio bitrate
        if (request.TargetAudioBitrate.HasValue)
        {
            args.Append($"-b:a {request.TargetAudioBitrate.Value} ");
        }

        // Video stream mapping (ensure video is included)
        args.Append("-map 0:v:0 ");

        // Audio stream selection with validation
        if (request.AudioStreamIndex.HasValue)
        {
            var audioIndex = request.AudioStreamIndex.Value;
            _logger.LogInformation("Using selected audio track {AudioIndex} for session {SessionId}", 
                audioIndex, request.SessionId);
            args.Append($"-map 0:a:{audioIndex} ");
        }
        else
        {
            _logger.LogInformation("No specific audio track selected, using first available for session {SessionId}", 
                request.SessionId);
            args.Append("-map 0:a:0 ");
        }
    }

    private void AddOutputFormatArgs(StringBuilder args, TranscodeRequest request)
    {
        switch (request.OutputFormat.ToLowerInvariant())
        {
            case "hls":
            case "m3u8":
                args.Append($"-hls_time {_settings.SegmentDuration} ");
                args.Append($"-hls_list_size {_settings.PlaylistLength} ");
                args.Append("-hls_flags delete_segments ");
                break;
            case "dash":
            case "mpd":
                args.Append($"-seg_duration {_settings.SegmentDuration} ");
                args.Append("-use_template 1 -use_timeline 1 ");
                break;
        }
    }

    private void AddEncodingPreset(StringBuilder args, string codec)
    {
        var preset = _settings.EncodingPreset.ToString().ToLowerInvariant();
        
        if (codec.Contains("x264") || codec.Contains("x265"))
        {
            // Use quality-focused presets for software encoding
            var qualityPreset = preset switch
            {
                "ultrafast" => "veryfast", // Upgrade ultrafast to veryfast for better quality
                "superfast" => "faster",   // Upgrade superfast to faster
                "veryfast" => "fast",      // Upgrade veryfast to fast
                "faster" => "medium",      // Upgrade faster to medium
                "fast" => "medium",        // Keep fast as medium
                "medium" => "slow",        // Upgrade medium to slow for better quality
                "slow" => "slow",          // Keep slow
                "slower" => "slower",      // Keep slower
                "veryslow" => "veryslow",  // Keep veryslow
                _ => "slow"                // Default to slow for quality
            };
            args.Append($"-preset {qualityPreset} ");
            
            // Add tune for quality
            if (codec.Contains("x264"))
            {
                args.Append("-tune film "); // Film tune for better quality on movies/shows
            }
        }
        else if (codec.Contains("nvenc"))
        {
            // Use higher quality NVENC presets
            var nvencPreset = preset switch
            {
                "ultrafast" => "p3",        // Upgrade from p1 to p3
                "superfast" => "p4",        // Upgrade from p2 to p4
                "veryfast" => "p5",         // Upgrade from p3 to p5
                "faster" => "p6",           // Upgrade from p4 to p6
                "fast" => "p6",             // Use p6 for fast (higher quality)
                "medium" => "p7",           // Use p7 for medium (high quality)
                "slow" => "p7",             // Use p7 for slow
                "slower" => "p7",           // Use p7 for slower
                "veryslow" => "p7",         // Use p7 for veryslow (highest quality)
                _ => "p6"                   // Default to p6 (high quality)
            };
            args.Append($"-preset {nvencPreset} ");
            
            // Add multipass for better quality
            args.Append("-multipass fullres "); // Full resolution multipass for quality
        }
        else if (codec.Contains("qsv"))
        {
            // Intel QuickSync presets - favor quality
            var qsvPreset = preset switch
            {
                "ultrafast" => "fast",      // Upgrade ultrafast
                "superfast" => "fast",      // Upgrade superfast
                "veryfast" => "medium",     // Upgrade veryfast
                "faster" => "medium",       // Upgrade faster
                "fast" => "slow",           // Upgrade fast to slow for quality
                "medium" => "slow",         // Upgrade medium to slow
                "slow" => "slower",         // Upgrade slow to slower
                "slower" => "veryslow",     // Upgrade slower to veryslow
                "veryslow" => "veryslow",   // Keep veryslow
                _ => "slow"                 // Default to slow for quality
            };
            args.Append($"-preset {qsvPreset} ");
        }
        else if (codec.Contains("amf"))
        {
            // AMD AMF presets - favor quality
            var amfPreset = preset switch
            {
                "ultrafast" => "balanced",  // Upgrade from speed to balanced
                "superfast" => "balanced",  // Upgrade from speed to balanced
                "veryfast" => "balanced",   // Upgrade from speed to balanced
                "faster" => "quality",      // Upgrade from speed to quality
                "fast" => "quality",        // Use quality for fast
                "medium" => "quality",      // Use quality for medium
                "slow" => "quality",        // Keep quality
                "slower" => "quality",      // Keep quality
                "veryslow" => "quality",    // Keep quality
                _ => "quality"              // Default to quality
            };
            args.Append($"-quality {amfPreset} ");
        }
    }

    private List<string> GetToneMappingFilters(HwAccelMethod hwAccel)
    {
        var algorithm = _settings.ToneMappingAlgorithm.ToString().ToLowerInvariant();
        
        if (hwAccel == HwAccelMethod.Vaapi)
        {
            return new List<string> { "tonemap_vaapi=format=nv12:p=bt709:t=bt709:m=bt709" };
        }
        else if (hwAccel == HwAccelMethod.Nvenc)
        {
            // Simplified tone mapping for NVENC - avoid complex zscale chains
            return new List<string> 
            { 
                "hwdownload",
                "format=nv12",
                $"tonemap={algorithm}:desat=0",
                "format=yuv420p",
                "hwupload_cuda"
            };
        }
        else
        {
            // Software tone mapping
            return new List<string> 
            { 
                "zscale=t=linear:npl=100",
                "format=gbrpf32le",
                "zscale=p=bt709",
                $"tonemap={algorithm}:desat=0",
                "zscale=t=bt709:m=bt709:r=tv",
                "format=yuv420p"
            };
        }
    }

    private string GetOutputFormat(string container)
    {
        return container.ToLowerInvariant() switch
        {
            "mp4" => "mp4",
            "mkv" => "matroska", 
            "webm" => "webm",
            "ts" or "mpegts" => "mpegts",
            "hls" or "m3u8" => "hls",
            "dash" or "mpd" => "dash",
            _ => _settings.PreferMpegTsForSeeking ? "mpegts" : "mp4" // Use MPEG-TS for better seeking if enabled
        };
    }



    private string GetOptimalVideoCodec(string? targetCodec, HwAccelMethod hwAccelMethod)
    {
        if (string.IsNullOrEmpty(targetCodec) || targetCodec == "copy" || !_settings.EnableHardwareAcceleration)
        {
            return targetCodec ?? "libx264";
        }

        // Map software codecs to hardware-accelerated equivalents
        return hwAccelMethod switch
        {
            HwAccelMethod.Nvenc => targetCodec.ToLowerInvariant() switch
            {
                "libx264" or "h264" => "h264_nvenc",
                "libx265" or "hevc" => "hevc_nvenc",
                "av1" => "av1_nvenc", // If supported by newer drivers
                _ => targetCodec
            },
            HwAccelMethod.QuickSync => targetCodec.ToLowerInvariant() switch
            {
                "libx264" or "h264" => "h264_qsv",
                "libx265" or "hevc" => "hevc_qsv",
                "av1" => "av1_qsv", // If supported
                _ => targetCodec
            },
            HwAccelMethod.Amf => targetCodec.ToLowerInvariant() switch
            {
                "libx264" or "h264" => "h264_amf",
                "libx265" or "hevc" => "hevc_amf",
                _ => targetCodec
            },
            HwAccelMethod.Vaapi => targetCodec.ToLowerInvariant() switch
            {
                "libx264" or "h264" => "h264_vaapi",
                "libx265" or "hevc" => "hevc_vaapi",
                _ => targetCodec
            },
            HwAccelMethod.VideoToolbox => targetCodec.ToLowerInvariant() switch
            {
                "libx264" or "h264" => "h264_videotoolbox",
                "libx265" or "hevc" => "hevc_videotoolbox",
                _ => targetCodec
            },
            _ => targetCodec
        };
    }

    private string FindFFmpegPath()
    {
        if (!string.IsNullOrEmpty(_settings.FFmpegPath) && File.Exists(_settings.FFmpegPath))
        {
            return _settings.FFmpegPath;
        }

        var ffmpegCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
        
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (pathVariable != null)
        {
            var paths = pathVariable.Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, ffmpegCommand);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return ffmpegCommand;
    }
}