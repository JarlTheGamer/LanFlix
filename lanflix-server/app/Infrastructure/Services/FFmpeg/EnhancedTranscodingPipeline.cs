using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
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

            while (!process.HasExited && !cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await outputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                
                if (bytesRead == 0)
                {
                    // Check if process is still running
                    if (process.HasExited)
                        break;
                    
                    // Small delay to prevent busy waiting
                    await Task.Delay(10, cancellationToken);
                    continue;
                }

                yield return new ReadOnlyMemory<byte>(buffer, 0, bytesRead);
            }

            // Wait for process to complete
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                _logger.LogError("FFmpeg process failed with exit code {ExitCode}: {Error}",
                    process.ExitCode, error);
                throw new InvalidOperationException($"Transcoding failed: {error}");
            }

            _logger.LogInformation("Transcoding completed successfully for session {SessionId}", request.SessionId);
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

    private string BuildFFmpegArguments(TranscodeRequest request)
    {
        var args = new StringBuilder();

        // Hardware acceleration setup (must be before input)
        if (request.HwAccelMethod != HwAccelMethod.None)
        {
            AddHardwareAccelerationArgs(args, request.HwAccelMethod);
        }

        // Input file
        if (request.StartPosition.HasValue && request.StartPosition.Value > 0)
        {
            args.Append($"-ss {request.StartPosition.Value:F3} ");
        }
        
        args.Append($"-i \"{request.InputPath}\" ");

        // Video encoding
        AddVideoEncodingArgs(args, request);

        // Audio encoding
        AddAudioEncodingArgs(args, request);

        // Subtitle handling
        if (request.SubtitleStreamIndex.HasValue)
        {
            args.Append($"-map 0:s:{request.SubtitleStreamIndex.Value} ");
        }

        // Output format and container
        AddOutputFormatArgs(args, request);

        // Threading
        if (_settings.ThreadCount > 0)
        {
            args.Append($"-threads {_settings.ThreadCount} ");
        }

        // General options
        args.Append("-avoid_negative_ts make_zero ");
        args.Append("-fflags +genpts ");
        args.Append("-f ");
        
        // Determine output format
        var outputFormat = GetOutputFormat(request.OutputFormat);
        args.Append($"{outputFormat} ");

        // Output to stdout
        args.Append("pipe:1");

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
                // Only use cuda output format if we're doing scaling or other GPU operations
                args.Append("-hwaccel_output_format cuda ");
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

        // Video bitrate
        if (request.TargetVideoBitrate.HasValue)
        {
            args.Append($"-b:v {request.TargetVideoBitrate.Value} ");
            args.Append($"-maxrate {request.TargetVideoBitrate.Value * 1.2:F0} ");
            args.Append($"-bufsize {request.TargetVideoBitrate.Value * 2:F0} ");
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
                videoFilters.Add($"scale_cuda={request.TargetWidth.Value}:{request.TargetHeight.Value}");
            }
            else
            {
                videoFilters.Add($"scale={request.TargetWidth.Value}:{request.TargetHeight.Value}");
            }
        }

        // HDR tone mapping
        if (request.SourceMedia.Video.IsHDR && _settings.EnableToneMapping)
        {
            var toneMappingFilters = GetToneMappingFilters(request.HwAccelMethod);
            videoFilters.AddRange(toneMappingFilters);
        }

        // Apply video filters if any
        if (videoFilters.Count > 0)
        {
            args.Append($"-vf \"{string.Join(",", videoFilters)}\" ");
        }

        // Encoding preset
        if (!string.IsNullOrEmpty(videoCodec))
        {
            AddEncodingPreset(args, videoCodec);
        }

        // Quality settings
        if (_settings.TargetQuality.HasValue)
        {
            var codec = GetOptimalVideoCodec(request.TargetVideoCodec, request.HwAccelMethod);
            if (codec.Contains("x264") || codec.Contains("x265"))
            {
                args.Append($"-crf {_settings.TargetQuality.Value} ");
            }
            else if (codec.Contains("nvenc"))
            {
                // NVENC uses different quality scale (0-51, but different meaning)
                var nvencCq = Math.Max(0, Math.Min(51, _settings.TargetQuality.Value));
                args.Append($"-cq {nvencCq} ");
            }
            else if (codec.Contains("qsv"))
            {
                // QuickSync uses global_quality
                var qsvQuality = Math.Max(1, Math.Min(51, _settings.TargetQuality.Value));
                args.Append($"-global_quality {qsvQuality} ");
            }
            else if (codec.Contains("amf"))
            {
                // AMF uses qp_i, qp_p, qp_b
                var amfQp = Math.Max(0, Math.Min(51, _settings.TargetQuality.Value));
                args.Append($"-qp_i {amfQp} -qp_p {amfQp} -qp_b {amfQp} ");
            }
        }

        // B-frames
        if (_settings.EnableBFrames)
        {
            var codec = GetOptimalVideoCodec(request.TargetVideoCodec, request.HwAccelMethod);
            if (codec.Contains("nvenc"))
            {
                args.Append("-bf 3 -b_ref_mode middle ");
            }
            else if (codec.Contains("qsv"))
            {
                args.Append("-bf 3 ");
            }
            else if (codec.Contains("amf"))
            {
                args.Append("-bf 3 ");
            }
            else if (!codec.Contains("vaapi")) // VAAPI doesn't always support B-frames well
            {
                args.Append("-bf 3 ");
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

        // Audio bitrate
        if (request.TargetAudioBitrate.HasValue)
        {
            args.Append($"-b:a {request.TargetAudioBitrate.Value} ");
        }

        // Audio stream selection
        if (request.AudioStreamIndex.HasValue)
        {
            args.Append($"-map 0:a:{request.AudioStreamIndex.Value} ");
        }
        else
        {
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
            args.Append($"-preset {preset} ");
        }
        else if (codec.Contains("nvenc"))
        {
            var nvencPreset = preset switch
            {
                "ultrafast" => "p1",
                "superfast" => "p2", 
                "veryfast" => "p3",
                "faster" => "p4",
                "fast" => "p5",
                "medium" => "p6",
                "slow" => "p7",
                "slower" => "p7", // Map slower to p7 as well
                "veryslow" => "p7", // Map veryslow to p7 as well
                _ => "p6"
            };
            args.Append($"-preset {nvencPreset} ");
        }
        else if (codec.Contains("qsv"))
        {
            // Intel QuickSync presets
            var qsvPreset = preset switch
            {
                "ultrafast" => "veryfast",
                "superfast" => "veryfast",
                "veryfast" => "veryfast", 
                "faster" => "faster",
                "fast" => "fast",
                "medium" => "medium",
                "slow" => "slow",
                "slower" => "slower",
                "veryslow" => "veryslow",
                _ => "medium"
            };
            args.Append($"-preset {qsvPreset} ");
        }
        else if (codec.Contains("amf"))
        {
            // AMD AMF presets
            var amfPreset = preset switch
            {
                "ultrafast" => "speed",
                "superfast" => "speed",
                "veryfast" => "speed",
                "faster" => "speed",
                "fast" => "balanced",
                "medium" => "balanced",
                "slow" => "quality",
                "slower" => "quality",
                "veryslow" => "quality",
                _ => "balanced"
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
        else
        {
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
            _ => "mpegts"
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