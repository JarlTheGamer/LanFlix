using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.FFmpeg;

/// <summary>
/// Enhanced hardware acceleration detector for modern transcoding
/// Supports Intel, AMD, Nvidia, Apple, and Rockchip GPUs
/// </summary>
public class EnhancedHardwareAccelerationDetector : IHardwareAccelerationDetector
{
    private readonly ILogger<EnhancedHardwareAccelerationDetector> _logger;
    private readonly string _ffmpegPath;
    private HardwareAcceleration? _cachedCapabilities;

    public EnhancedHardwareAccelerationDetector(ILogger<EnhancedHardwareAccelerationDetector> logger)
    {
        _logger = logger;
        _ffmpegPath = FindFFmpegPath();
    }

    public async Task<HardwareAcceleration> DetectAsync()
    {
        if (_cachedCapabilities != null)
        {
            return _cachedCapabilities;
        }

        _logger.LogInformation("Detecting hardware acceleration capabilities...");

        var capabilities = new HardwareAcceleration();

        try
        {
            // Detect NVIDIA NVENC
            var nvencCapabilities = await DetectNvencAsync();
            capabilities = capabilities with { Nvenc = nvencCapabilities };

            // Detect Intel QuickSync
            var quickSyncCapabilities = await DetectQuickSyncAsync();
            capabilities = capabilities with { QuickSync = quickSyncCapabilities };

            // Detect AMD AMF
            var amfCapabilities = await DetectAmfAsync();
            capabilities = capabilities with { Amf = amfCapabilities };

            // Detect VAAPI (Linux)
            var vaapiCapabilities = await DetectVaapiAsync();
            capabilities = capabilities with { Vaapi = vaapiCapabilities };

            // Detect VideoToolbox (macOS)
            var videoToolboxCapabilities = await DetectVideoToolboxAsync();
            capabilities = capabilities with { VideoToolbox = videoToolboxCapabilities };

            // Detect Rockchip MPP
            var rockchipCapabilities = await DetectRockchipAsync();
            capabilities = capabilities with { Rockchip = rockchipCapabilities };

            // Determine preferred method and additional capabilities
            var preferredMethod = DeterminePreferredMethod(capabilities);
            var maxSessions = DetermineMaxSessions(capabilities, preferredMethod);
            var supportsToneMapping = DetermineToneMappingSupport(capabilities, preferredMethod);

            capabilities = capabilities with 
            { 
                PreferredMethod = preferredMethod,
                MaxConcurrentSessions = maxSessions,
                SupportsToneMapping = supportsToneMapping
            };

            _cachedCapabilities = capabilities;

            _logger.LogInformation("Hardware acceleration detection complete. Preferred method: {Method}, Max sessions: {Sessions}, Tone mapping: {ToneMapping}",
                preferredMethod, maxSessions, supportsToneMapping);

            return capabilities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting hardware acceleration capabilities");
            return new HardwareAcceleration { PreferredMethod = HwAccelMethod.None };
        }
    }

    private async Task<NvencCapabilities> DetectNvencAsync()
    {
        try
        {
            // Check for NVIDIA GPU using nvidia-smi or FFmpeg encoders
            var encoders = await GetAvailableEncodersAsync();
            
            var hasH264Nvenc = encoders.Contains("h264_nvenc");
            var hasHevcNvenc = encoders.Contains("hevc_nvenc");
            var hasAv1Nvenc = encoders.Contains("av1_nvenc");

            _logger.LogInformation("NVENC detection - H264: {H264}, HEVC: {HEVC}, AV1: {AV1}", 
                hasH264Nvenc, hasHevcNvenc, hasAv1Nvenc);

            if (!hasH264Nvenc && !hasHevcNvenc)
            {
                _logger.LogInformation("NVENC not available - no NVENC encoders found");
                return new NvencCapabilities { IsAvailable = false };
            }

            // Try to get GPU info using nvidia-smi
            var gpuInfo = await GetNvidiaGpuInfoAsync();

            return new NvencCapabilities
            {
                IsAvailable = true,
                GpuName = gpuInfo.GpuName,
                DriverVersion = gpuInfo.DriverVersion,
                SupportsH264 = hasH264Nvenc,
                SupportsHevc = hasHevcNvenc,
                SupportsAv1 = hasAv1Nvenc,
                SupportsBFrames = true,
                Supports10Bit = hasHevcNvenc, // HEVC NVENC supports 10-bit
                SupportsHdr = hasHevcNvenc,
                MaxSessions = DetermineNvencMaxSessions(gpuInfo.GpuName),
                SupportedProfiles = GetNvencSupportedProfiles()
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "NVENC detection failed");
            return new NvencCapabilities { IsAvailable = false };
        }
    }

    private async Task<QuickSyncCapabilities> DetectQuickSyncAsync()
    {
        try
        {
            var encoders = await GetAvailableEncodersAsync();
            
            var hasH264Qsv = encoders.Contains("h264_qsv");
            var hasHevcQsv = encoders.Contains("hevc_qsv");
            var hasAv1Qsv = encoders.Contains("av1_qsv");
            var hasVp9Qsv = encoders.Contains("vp9_qsv");

            if (!hasH264Qsv && !hasHevcQsv)
            {
                return new QuickSyncCapabilities { IsAvailable = false };
            }

            // Try to get Intel GPU info
            var gpuInfo = await GetIntelGpuInfoAsync();

            return new QuickSyncCapabilities
            {
                IsAvailable = true,
                GpuName = gpuInfo,
                SupportsH264 = hasH264Qsv,
                SupportsHevc = hasHevcQsv,
                SupportsAv1 = hasAv1Qsv,
                SupportsVp9 = hasVp9Qsv,
                Supports10Bit = hasHevcQsv,
                SupportsHdr = hasHevcQsv,
                SupportsLowPower = true,
                MaxSessions = 4, // Intel typically supports multiple sessions
                SupportedProfiles = GetQuickSyncSupportedProfiles()
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "QuickSync detection failed");
            return new QuickSyncCapabilities { IsAvailable = false };
        }
    }

    private async Task<AmfCapabilities> DetectAmfAsync()
    {
        try
        {
            var encoders = await GetAvailableEncodersAsync();
            
            var hasH264Amf = encoders.Contains("h264_amf");
            var hasHevcAmf = encoders.Contains("hevc_amf");
            var hasAv1Amf = encoders.Contains("av1_amf");

            if (!hasH264Amf && !hasHevcAmf)
            {
                return new AmfCapabilities { IsAvailable = false };
            }

            // Try to get AMD GPU info
            var gpuInfo = await GetAmdGpuInfoAsync();

            return new AmfCapabilities
            {
                IsAvailable = true,
                GpuName = gpuInfo.GpuName,
                DriverVersion = gpuInfo.DriverVersion,
                SupportsH264 = hasH264Amf,
                SupportsHevc = hasHevcAmf,
                SupportsAv1 = hasAv1Amf,
                Supports10Bit = hasHevcAmf,
                SupportsHdr = hasHevcAmf,
                SupportsPreAnalysis = true,
                MaxSessions = 2, // AMD typically supports 2 concurrent sessions
                SupportedProfiles = GetAmfSupportedProfiles()
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "AMF detection failed");
            return new AmfCapabilities { IsAvailable = false };
        }
    }

    private async Task<VaapiCapabilities> DetectVaapiAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new VaapiCapabilities { IsAvailable = false };
        }

        try
        {
            var encoders = await GetAvailableEncodersAsync();
            
            var hasH264Vaapi = encoders.Contains("h264_vaapi");
            var hasHevcVaapi = encoders.Contains("hevc_vaapi");
            var hasVp8Vaapi = encoders.Contains("vp8_vaapi");
            var hasVp9Vaapi = encoders.Contains("vp9_vaapi");
            var hasAv1Vaapi = encoders.Contains("av1_vaapi");

            if (!hasH264Vaapi && !hasHevcVaapi)
            {
                return new VaapiCapabilities { IsAvailable = false };
            }

            // Check for VAAPI device
            var devicePath = await FindVaapiDeviceAsync();

            return new VaapiCapabilities
            {
                IsAvailable = true,
                DevicePath = devicePath,
                DriverName = await GetVaapiDriverNameAsync(devicePath),
                SupportsH264 = hasH264Vaapi,
                SupportsHevc = hasHevcVaapi,
                SupportsVp8 = hasVp8Vaapi,
                SupportsVp9 = hasVp9Vaapi,
                SupportsAv1 = hasAv1Vaapi,
                Supports10Bit = hasHevcVaapi,
                SupportsHdr = hasHevcVaapi,
                MaxSessions = 2,
                SupportedProfiles = GetVaapiSupportedProfiles()
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "VAAPI detection failed");
            return new VaapiCapabilities { IsAvailable = false };
        }
    }

    private async Task<VideoToolboxCapabilities> DetectVideoToolboxAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new VideoToolboxCapabilities { IsAvailable = false };
        }

        try
        {
            var encoders = await GetAvailableEncodersAsync();
            
            var hasH264Vt = encoders.Contains("h264_videotoolbox");
            var hasHevcVt = encoders.Contains("hevc_videotoolbox");
            var hasProResVt = encoders.Contains("prores_videotoolbox");

            if (!hasH264Vt && !hasHevcVt)
            {
                return new VideoToolboxCapabilities { IsAvailable = false };
            }

            var deviceModel = await GetMacDeviceModelAsync();

            return new VideoToolboxCapabilities
            {
                IsAvailable = true,
                DeviceModel = deviceModel,
                SupportsH264 = hasH264Vt,
                SupportsHevc = hasHevcVt,
                SupportsProRes = hasProResVt,
                Supports10Bit = hasHevcVt,
                SupportsHdr = hasHevcVt,
                SupportsAlpha = hasProResVt,
                MaxSessions = 1, // VideoToolbox typically supports 1 session
                SupportedProfiles = GetVideoToolboxSupportedProfiles()
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "VideoToolbox detection failed");
            return new VideoToolboxCapabilities { IsAvailable = false };
        }
    }

    private async Task<RockchipCapabilities> DetectRockchipAsync()
    {
        try
        {
            var encoders = await GetAvailableEncodersAsync();
            
            var hasH264Rkmpp = encoders.Contains("h264_rkmpp");
            var hasHevcRkmpp = encoders.Contains("hevc_rkmpp");
            var hasVp8Rkmpp = encoders.Contains("vp8_rkmpp");
            var hasVp9Rkmpp = encoders.Contains("vp9_rkmpp");

            if (!hasH264Rkmpp && !hasHevcRkmpp)
            {
                return new RockchipCapabilities { IsAvailable = false };
            }

            var chipModel = await GetRockchipChipModelAsync();

            return new RockchipCapabilities
            {
                IsAvailable = true,
                ChipModel = chipModel,
                SupportsH264 = hasH264Rkmpp,
                SupportsHevc = hasHevcRkmpp,
                SupportsVp8 = hasVp8Rkmpp,
                SupportsVp9 = hasVp9Rkmpp,
                Supports10Bit = hasHevcRkmpp,
                MaxSessions = 1,
                SupportedProfiles = GetRockchipSupportedProfiles()
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Rockchip detection failed");
            return new RockchipCapabilities { IsAvailable = false };
        }
    }

    private async Task<string[]> GetAvailableEncodersAsync()
    {
        try
        {
            var output = await ExecuteFFmpegAsync("-encoders");
            var encoders = new List<string>();

            _logger.LogInformation("FFmpeg -encoders output length: {Length} characters", output.Length);
            _logger.LogInformation("FFmpeg -encoders output preview: {Preview}", 
                output.Length > 500 ? output.Substring(0, 500) + "..." : output);

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            _logger.LogInformation("Processing {LineCount} lines from FFmpeg output", lines.Length);

            foreach (var line in lines)
            {
                // Log first few lines to see the format
                if (encoders.Count < 5)
                {
                    _logger.LogInformation("Processing line: '{Line}'", line);
                }

                // Look for video encoders - they start with "V" (format: "V....D encodername description")
                if (line.StartsWith(" V") && line.Length > 7)
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var encoderName = parts[1];
                        encoders.Add(encoderName);
                        
                        if (encoders.Count <= 10)
                        {
                            _logger.LogInformation("Found encoder: '{Encoder}' from line: '{Line}'", encoderName, line);
                        }
                    }
                }
            }

            _logger.LogInformation("Found {Count} video encoders: {Encoders}", 
                encoders.Count, string.Join(", ", encoders.Take(10))); // Log first 10 encoders

            // Log specifically hardware encoders
            var hwEncoders = encoders.Where(e => 
                e.Contains("nvenc") || e.Contains("qsv") || e.Contains("amf") || 
                e.Contains("vaapi") || e.Contains("videotoolbox") || e.Contains("rkmpp")).ToList();
            
            if (hwEncoders.Any())
            {
                _logger.LogInformation("Hardware encoders found: {HwEncoders}", string.Join(", ", hwEncoders));
            }
            else
            {
                _logger.LogWarning("No hardware encoders found in FFmpeg build");
            }

            return encoders.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available encoders");
            return Array.Empty<string>();
        }
    }

    private async Task<string> ExecuteFFmpegAsync(string arguments)
    {
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
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return string.IsNullOrEmpty(output) ? error : output;
    }

    private string FindFFmpegPath()
    {
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

    private HwAccelMethod DeterminePreferredMethod(HardwareAcceleration capabilities)
    {
        // Priority order: NVENC > QuickSync > AMF > VideoToolbox > VAAPI > Rockchip
        if (capabilities.Nvenc.IsAvailable) return HwAccelMethod.Nvenc;
        if (capabilities.QuickSync.IsAvailable) return HwAccelMethod.QuickSync;
        if (capabilities.Amf.IsAvailable) return HwAccelMethod.Amf;
        if (capabilities.VideoToolbox.IsAvailable) return HwAccelMethod.VideoToolbox;
        if (capabilities.Vaapi.IsAvailable) return HwAccelMethod.Vaapi;
        if (capabilities.Rockchip.IsAvailable) return HwAccelMethod.Rockchip;
        
        return HwAccelMethod.None;
    }

    private int DetermineMaxSessions(HardwareAcceleration capabilities, HwAccelMethod preferredMethod)
    {
        return preferredMethod switch
        {
            HwAccelMethod.Nvenc => capabilities.Nvenc.MaxSessions,
            HwAccelMethod.QuickSync => capabilities.QuickSync.MaxSessions,
            HwAccelMethod.Amf => capabilities.Amf.MaxSessions,
            HwAccelMethod.VideoToolbox => capabilities.VideoToolbox.MaxSessions,
            HwAccelMethod.Vaapi => capabilities.Vaapi.MaxSessions,
            HwAccelMethod.Rockchip => capabilities.Rockchip.MaxSessions,
            _ => 1
        };
    }

    private bool DetermineToneMappingSupport(HardwareAcceleration capabilities, HwAccelMethod preferredMethod)
    {
        return preferredMethod switch
        {
            HwAccelMethod.Nvenc => capabilities.Nvenc.SupportsHdr,
            HwAccelMethod.QuickSync => capabilities.QuickSync.SupportsHdr,
            HwAccelMethod.Amf => capabilities.Amf.SupportsHdr,
            HwAccelMethod.VideoToolbox => capabilities.VideoToolbox.SupportsHdr,
            HwAccelMethod.Vaapi => capabilities.Vaapi.SupportsHdr,
            _ => false
        };
    }

    // Placeholder methods for GPU info detection
    private async Task<(string? GpuName, string? DriverVersion)> GetNvidiaGpuInfoAsync()
    {
        // Implementation would use nvidia-smi or similar
        return (null, null);
    }

    private async Task<string?> GetIntelGpuInfoAsync()
    {
        // Implementation would detect Intel GPU
        return null;
    }

    private async Task<(string? GpuName, string? DriverVersion)> GetAmdGpuInfoAsync()
    {
        // Implementation would detect AMD GPU
        return (null, null);
    }

    private async Task<string?> FindVaapiDeviceAsync()
    {
        // Implementation would find VAAPI device path
        return "/dev/dri/renderD128";
    }

    private async Task<string?> GetVaapiDriverNameAsync(string? devicePath)
    {
        // Implementation would get VAAPI driver name
        return null;
    }

    private async Task<string?> GetMacDeviceModelAsync()
    {
        // Implementation would get Mac device model
        return null;
    }

    private async Task<string?> GetRockchipChipModelAsync()
    {
        // Implementation would get Rockchip chip model
        return null;
    }

    private int DetermineNvencMaxSessions(string? gpuName)
    {
        // Different NVIDIA GPUs support different numbers of concurrent sessions
        return gpuName?.ToLowerInvariant() switch
        {
            var name when name?.Contains("rtx") == true => 3,
            var name when name?.Contains("gtx") == true => 2,
            _ => 2
        };
    }

    private string[] GetNvencSupportedProfiles() => new[] { "baseline", "main", "high" };
    private string[] GetQuickSyncSupportedProfiles() => new[] { "baseline", "main", "high" };
    private string[] GetAmfSupportedProfiles() => new[] { "baseline", "main", "high" };
    private string[] GetVaapiSupportedProfiles() => new[] { "baseline", "main", "high" };
    private string[] GetVideoToolboxSupportedProfiles() => new[] { "baseline", "main", "high" };
    private string[] GetRockchipSupportedProfiles() => new[] { "baseline", "main" };
}