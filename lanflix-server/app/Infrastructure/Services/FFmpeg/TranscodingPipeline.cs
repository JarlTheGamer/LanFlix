using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.FFmpeg;

/// <summary>
/// FFmpeg-based transcoding pipeline with hardware acceleration support
/// </summary>
public class TranscodingPipeline : ITranscodingPipeline
{
    private readonly ILogger<TranscodingPipeline> _logger;
    private readonly string _ffmpegPath;
    private const int BufferSize = 81920; // 80KB chunks

    public TranscodingPipeline(ILogger<TranscodingPipeline> logger)
    {
        _logger = logger;
        _ffmpegPath = FindFFmpegPath();
    }

    public async IAsyncEnumerable<ReadOnlyMemory<byte>> StreamAsync(
        TranscodeRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var arguments = BuildFFmpegCommand(request);
        
        _logger.LogInformation(
            "Starting transcoding: {Mode}, Input: {Input}, HwAccel: {HwAccel}",
            request.Mode,
            Path.GetFileName(request.InputPath),
            request.HwAccelMethod);

        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        
        // Rent buffer from ArrayPool for efficient memory usage
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        
        Process? processToCleanup = null;
        
        try
        {
            process.Start();
            processToCleanup = process;

            // Start reading stderr in background for logging
            _ = Task.Run(async () => await LogFFmpegOutputAsync(process, cancellationToken), cancellationToken);

            var stdout = process.StandardOutput.BaseStream;
            int bytesRead;

            while ((bytesRead = await stdout.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken)) > 0)
            {
                // Create a copy of the data to yield
                var data = new byte[bytesRead];
                Array.Copy(buffer, 0, data, 0, bytesRead);
                
                yield return new ReadOnlyMemory<byte>(data);

                // Check if process has exited unexpectedly
                if (process.HasExited && process.ExitCode != 0)
                {
                    _logger.LogError("FFmpeg process exited unexpectedly with code {ExitCode}", process.ExitCode);
                    throw new InvalidOperationException($"Transcoding failed with exit code {process.ExitCode}");
                }
            }

            // Wait for process to complete
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogError("FFmpeg process failed with exit code {ExitCode}", process.ExitCode);
                throw new InvalidOperationException($"Transcoding failed with exit code {process.ExitCode}");
            }

            _logger.LogInformation("Transcoding completed successfully");
            processToCleanup = null; // Successfully completed, no cleanup needed
        }
        finally
        {
            // Return buffer to pool
            ArrayPool<byte>.Shared.Return(buffer);
            
            // Cleanup process if needed
            if (processToCleanup != null && !processToCleanup.HasExited)
            {
                try
                {
                    processToCleanup.Kill(entireProcessTree: true);
                    _logger.LogInformation("FFmpeg process terminated during cleanup");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to kill FFmpeg process during cleanup");
                }
            }
        }
    }

    private string FindFFmpegPath()
    {
        var ffmpegCommand = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        
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

    private string BuildFFmpegCommand(TranscodeRequest request)
    {
        var args = new List<string>();

        // Hardware acceleration input options
        if (request.HwAccelMethod != HwAccelMethod.None)
        {
            AddHardwareAccelInputArgs(args, request.HwAccelMethod);
        }

        // Input file
        args.Add($"-i \"{request.InputPath}\"");

        // Seek position (if specified)
        if (request.StartPosition.HasValue && request.StartPosition.Value > 0)
        {
            args.Add($"-ss {request.StartPosition.Value:F3}");
        }

        // Build encoding arguments based on mode
        switch (request.Mode)
        {
            case StreamingMode.DirectPlay:
                // Should not reach here, but handle it
                args.Add("-c copy");
                break;

            case StreamingMode.DirectStream:
                // Remux only - copy all streams
                args.Add("-c copy");
                break;

            case StreamingMode.TranscodeVideo:
                // Transcode video, copy audio
                AddVideoTranscodingArgs(args, request);
                args.Add("-c:a copy");
                break;

            case StreamingMode.FullTranscode:
                // Transcode both video and audio
                AddVideoTranscodingArgs(args, request);
                AddAudioTranscodingArgs(args, request);
                break;
        }

        // Stream selection
        if (request.AudioStreamIndex.HasValue)
        {
            args.Add($"-map 0:v:0 -map 0:a:{request.AudioStreamIndex.Value}");
        }

        // Output format
        args.Add($"-f {request.OutputFormat}");

        // Additional options for streaming
        args.Add("-movflags frag_keyframe+empty_moov");
        args.Add("-avoid_negative_ts make_zero");
        
        // Output to stdout
        args.Add("pipe:1");

        return string.Join(" ", args);
    }

    private void AddHardwareAccelInputArgs(List<string> args, HwAccelMethod method)
    {
        switch (method)
        {
            case HwAccelMethod.Nvenc:
                args.Add("-hwaccel cuda");
                args.Add("-hwaccel_output_format cuda");
                break;

            case HwAccelMethod.QuickSync:
                args.Add("-hwaccel qsv");
                args.Add("-hwaccel_output_format qsv");
                break;

            case HwAccelMethod.Amf:
                args.Add("-hwaccel d3d11va");
                args.Add("-hwaccel_output_format d3d11");
                break;

            case HwAccelMethod.Vaapi:
                args.Add("-hwaccel vaapi");
                args.Add("-hwaccel_output_format vaapi");
                args.Add("-vaapi_device /dev/dri/renderD128");
                break;

            case HwAccelMethod.VideoToolbox:
                args.Add("-hwaccel videotoolbox");
                break;
        }
    }

    private void AddVideoTranscodingArgs(List<string> args, TranscodeRequest request)
    {
        var videoCodec = DetermineVideoEncoder(request);
        args.Add($"-c:v {videoCodec}");

        // Preset for quality/speed tradeoff
        if (request.HwAccelMethod == HwAccelMethod.Nvenc)
        {
            args.Add("-preset p4"); // Balanced preset for NVENC
        }
        else if (request.HwAccelMethod == HwAccelMethod.None)
        {
            args.Add("-preset medium"); // Software encoding preset
        }

        // Bitrate
        var bitrate = request.TargetVideoBitrate ?? 8_000_000; // Default 8 Mbps
        args.Add($"-b:v {bitrate}");
        args.Add($"-maxrate {bitrate * 1.2}");
        args.Add($"-bufsize {bitrate * 2}");

        // Resolution scaling
        if (request.TargetWidth.HasValue && request.TargetHeight.HasValue)
        {
            if (request.HwAccelMethod == HwAccelMethod.Nvenc)
            {
                args.Add($"-vf scale_cuda={request.TargetWidth}:{request.TargetHeight}");
            }
            else if (request.HwAccelMethod == HwAccelMethod.Vaapi)
            {
                args.Add($"-vf scale_vaapi=w={request.TargetWidth}:h={request.TargetHeight}");
            }
            else
            {
                args.Add($"-vf scale={request.TargetWidth}:{request.TargetHeight}");
            }
        }

        // Keyframe interval for seeking
        args.Add("-g 60"); // Keyframe every 2 seconds at 30fps
    }

    private void AddAudioTranscodingArgs(List<string> args, TranscodeRequest request)
    {
        var audioCodec = request.TargetAudioCodec ?? "aac";
        args.Add($"-c:a {audioCodec}");

        var audioBitrate = request.TargetAudioBitrate ?? 192_000; // Default 192 kbps
        args.Add($"-b:a {audioBitrate}");

        // AAC-specific options
        if (audioCodec == "aac")
        {
            args.Add("-ac 2"); // Stereo output
        }
    }

    private string DetermineVideoEncoder(TranscodeRequest request)
    {
        var targetCodec = request.TargetVideoCodec ?? "h264";

        // Map codec to encoder based on hardware acceleration
        return (targetCodec, request.HwAccelMethod) switch
        {
            ("h264", HwAccelMethod.Nvenc) => "h264_nvenc",
            ("h264", HwAccelMethod.QuickSync) => "h264_qsv",
            ("h264", HwAccelMethod.Amf) => "h264_amf",
            ("h264", HwAccelMethod.Vaapi) => "h264_vaapi",
            ("h264", HwAccelMethod.VideoToolbox) => "h264_videotoolbox",
            ("h264", _) => "libx264",

            ("hevc", HwAccelMethod.Nvenc) => "hevc_nvenc",
            ("hevc", HwAccelMethod.QuickSync) => "hevc_qsv",
            ("hevc", HwAccelMethod.Amf) => "hevc_amf",
            ("hevc", HwAccelMethod.Vaapi) => "hevc_vaapi",
            ("hevc", HwAccelMethod.VideoToolbox) => "hevc_videotoolbox",
            ("hevc", _) => "libx265",

            _ => "libx264" // Default fallback
        };
    }

    private async Task LogFFmpegOutputAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            var stderr = process.StandardError;
            string? line;

            while ((line = await stderr.ReadLineAsync(cancellationToken)) != null)
            {
                // Log FFmpeg output for debugging
                // FFmpeg writes progress and errors to stderr
                if (line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("failed", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("FFmpeg: {Output}", line);
                }
                else if (line.Contains("frame=") || line.Contains("time="))
                {
                    // Progress information - log at debug level
                    _logger.LogDebug("FFmpeg: {Output}", line);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading FFmpeg stderr");
        }
    }
}
