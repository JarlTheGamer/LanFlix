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
                        process.Kill();
                        _logger.LogWarning("Killed FFmpeg process for session {SessionId}", request.SessionId);
                    }
                    catch (Exception killEx)
                    {
                        _logger.LogWarning(killEx, "Failed to kill FFmpeg process for session {SessionId}", request.SessionId);
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
        switch (method)
        {
            case HwAccelMethod.Nvenc:
                args.Append("-hwaccel cuda -hwaccel_output_format cuda ");
                break;
            case HwAccelMethod.QuickSync:
                args.Append("-hwaccel qsv -hwaccel_output_format qsv ");
                break;
            case HwAccelMethod.Amf:
                args.Append("-hwaccel d3d11va ");
                break;
            case HwAccelMethod.Vaapi:
                args.Append("-hwaccel vaapi -hwaccel_device /dev/dri/renderD128 -hwaccel_output_format vaapi ");
                break;
            case HwAccelMethod.VideoToolbox:
                args.Append("-hwaccel videotoolbox ");
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

        // Video codec
        args.Append($"-c:v {request.TargetVideoCodec} ");

        // Video bitrate
        if (request.TargetVideoBitrate.HasValue)
        {
            args.Append($"-b:v {request.TargetVideoBitrate.Value} ");
            args.Append($"-maxrate {request.TargetVideoBitrate.Value * 1.2:F0} ");
            args.Append($"-bufsize {request.TargetVideoBitrate.Value * 2:F0} ");
        }

        // Resolution scaling
        if (request.TargetWidth.HasValue && request.TargetHeight.HasValue)
        {
            if (request.HwAccelMethod == HwAccelMethod.Vaapi)
            {
                args.Append($"-vf \"scale_vaapi={request.TargetWidth.Value}:{request.TargetHeight.Value}\" ");
            }
            else if (request.HwAccelMethod == HwAccelMethod.Nvenc)
            {
                args.Append($"-vf \"scale_cuda={request.TargetWidth.Value}:{request.TargetHeight.Value}\" ");
            }
            else
            {
                args.Append($"-vf \"scale={request.TargetWidth.Value}:{request.TargetHeight.Value}\" ");
            }
        }

        // Encoding preset
        AddEncodingPreset(args, request.TargetVideoCodec);

        // Quality settings
        if (_settings.TargetQuality.HasValue)
        {
            if (request.TargetVideoCodec.Contains("x264") || request.TargetVideoCodec.Contains("x265"))
            {
                args.Append($"-crf {_settings.TargetQuality.Value} ");
            }
        }

        // B-frames
        if (_settings.EnableBFrames && !request.TargetVideoCodec.Contains("nvenc"))
        {
            args.Append("-bf 3 ");
        }

        // HDR tone mapping
        if (request.SourceMedia.Video.IsHDR && _settings.EnableToneMapping)
        {
            AddToneMappingArgs(args, request.HwAccelMethod);
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
                _ => "p6"
            };
            args.Append($"-preset {nvencPreset} ");
        }
    }

    private void AddToneMappingArgs(StringBuilder args, HwAccelMethod hwAccel)
    {
        var algorithm = _settings.ToneMappingAlgorithm.ToString().ToLowerInvariant();
        
        if (hwAccel == HwAccelMethod.Vaapi)
        {
            args.Append($"-vf \"tonemap_vaapi=format=nv12:p=bt709:t=bt709:m=bt709\" ");
        }
        else
        {
            args.Append($"-vf \"zscale=t=linear:npl=100,format=gbrpf32le,zscale=p=bt709,tonemap={algorithm}:desat=0,zscale=t=bt709:m=bt709:r=tv,format=yuv420p\" ");
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