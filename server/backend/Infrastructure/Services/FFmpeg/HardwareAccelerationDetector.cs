using System.Diagnostics;
using System.Runtime.InteropServices;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.FFmpeg;

/// <summary>
/// Detects available hardware acceleration methods for FFmpeg transcoding
/// </summary>
public class HardwareAccelerationDetector : IHardwareAccelerationDetector
{
    private readonly ILogger<HardwareAccelerationDetector> _logger;
    private readonly string _ffmpegPath;
    private HwAccelCapabilities? _cachedCapabilities;

    public HardwareAccelerationDetector(ILogger<HardwareAccelerationDetector> logger)
    {
        _logger = logger;
        _ffmpegPath = FindFFmpegPath();
    }

    public async Task<HwAccelCapabilities> DetectAsync(CancellationToken cancellationToken = default)
    {
        // Return cached result if available
        if (_cachedCapabilities != null)
        {
            return _cachedCapabilities;
        }

        _logger.LogInformation("Detecting hardware acceleration capabilities...");

        var capabilities = new HwAccelCapabilities();

        // Get list of available encoders
        var encoders = await GetAvailableEncodersAsync(cancellationToken);

        // Test for NVIDIA NVENC
        capabilities.HasNvenc = encoders.Contains("h264_nvenc") || encoders.Contains("hevc_nvenc");
        if (capabilities.HasNvenc)
        {
            _logger.LogInformation("NVIDIA NVENC detected");
        }

        // Test for Intel QuickSync
        capabilities.HasQuickSync = encoders.Contains("h264_qsv") || encoders.Contains("hevc_qsv");
        if (capabilities.HasQuickSync)
        {
            _logger.LogInformation("Intel QuickSync detected");
        }

        // Test for AMD AMF
        capabilities.HasAmf = encoders.Contains("h264_amf") || encoders.Contains("hevc_amf");
        if (capabilities.HasAmf)
        {
            _logger.LogInformation("AMD AMF detected");
        }

        // Test for VAAPI (Linux only)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            capabilities.HasVaapi = encoders.Contains("h264_vaapi") || encoders.Contains("hevc_vaapi");
            if (capabilities.HasVaapi)
            {
                _logger.LogInformation("VAAPI detected");
            }
        }

        // Test for VideoToolbox (macOS only)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            capabilities.HasVideoToolbox = encoders.Contains("h264_videotoolbox") || encoders.Contains("hevc_videotoolbox");
            if (capabilities.HasVideoToolbox)
            {
                _logger.LogInformation("VideoToolbox detected");
            }
        }

        // Determine preferred method based on priority
        capabilities.PreferredMethod = DeterminePreferredMethod(capabilities);

        if (capabilities.IsAvailable)
        {
            _logger.LogInformation("Hardware acceleration available: {Method}", capabilities.PreferredMethod);
        }
        else
        {
            _logger.LogWarning("No hardware acceleration available, will use software encoding");
        }

        // Cache the result
        _cachedCapabilities = capabilities;

        return capabilities;
    }

    private string FindFFmpegPath()
    {
        var ffmpegCommand = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        
        // Check if ffmpeg is in PATH
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

    private async Task<HashSet<string>> GetAvailableEncodersAsync(CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = "-encoders -hide_banner",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        
        try
        {
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return ParseEncoders(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query FFmpeg encoders");
            return new HashSet<string>();
        }
    }

    private HashSet<string> ParseEncoders(string output)
    {
        var encoders = new HashSet<string>();
        var lines = output.Split('\n');
        var inEncoderSection = false;

        foreach (var line in lines)
        {
            // Skip until we find the encoder section
            if (line.Contains("Encoders:"))
            {
                inEncoderSection = true;
                continue;
            }

            if (!inEncoderSection || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Encoder lines start with " V" for video, " A" for audio, etc.
            // Format: " V..... libx264              H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10"
            var trimmed = line.Trim();
            if (trimmed.Length < 8)
            {
                continue;
            }

            // Extract encoder name (after the flags)
            var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var encoderName = parts[1];
                encoders.Add(encoderName);
            }
        }

        return encoders;
    }

    private HwAccelMethod DeterminePreferredMethod(HwAccelCapabilities capabilities)
    {
        // Priority order: NVENC > QuickSync > AMF > VAAPI > VideoToolbox
        // NVENC is generally the fastest and most efficient

        if (capabilities.HasNvenc)
        {
            return HwAccelMethod.Nvenc;
        }

        if (capabilities.HasQuickSync)
        {
            return HwAccelMethod.QuickSync;
        }

        if (capabilities.HasAmf)
        {
            return HwAccelMethod.Amf;
        }

        if (capabilities.HasVaapi)
        {
            return HwAccelMethod.Vaapi;
        }

        if (capabilities.HasVideoToolbox)
        {
            return HwAccelMethod.VideoToolbox;
        }

        return HwAccelMethod.None;
    }
}
