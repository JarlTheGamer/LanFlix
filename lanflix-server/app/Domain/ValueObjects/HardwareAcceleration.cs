namespace Lanflix.Domain.ValueObjects;

/// <summary>
/// Enhanced hardware acceleration capabilities for modern transcoding
/// Supports Intel, AMD, Nvidia, Apple, and Rockchip GPUs
/// </summary>
public record HardwareAcceleration
{
    /// <summary>
    /// NVIDIA NVENC encoder availability
    /// </summary>
    public NvencCapabilities Nvenc { get; init; } = new();

    /// <summary>
    /// Intel QuickSync Video capabilities
    /// </summary>
    public QuickSyncCapabilities QuickSync { get; init; } = new();

    /// <summary>
    /// AMD AMF (Advanced Media Framework) capabilities
    /// </summary>
    public AmfCapabilities Amf { get; init; } = new();

    /// <summary>
    /// VAAPI (Video Acceleration API) capabilities - Linux
    /// </summary>
    public VaapiCapabilities Vaapi { get; init; } = new();

    /// <summary>
    /// Apple VideoToolbox capabilities - macOS/iOS
    /// </summary>
    public VideoToolboxCapabilities VideoToolbox { get; init; } = new();

    /// <summary>
    /// Rockchip MPP (Media Process Platform) capabilities
    /// </summary>
    public RockchipCapabilities Rockchip { get; init; } = new();

    /// <summary>
    /// Preferred hardware acceleration method based on detection
    /// </summary>
    public HwAccelMethod PreferredMethod { get; init; } = HwAccelMethod.None;

    /// <summary>
    /// Whether any hardware acceleration is available
    /// </summary>
    public bool IsAvailable => PreferredMethod != HwAccelMethod.None;

    /// <summary>
    /// Number of hardware encoder sessions available
    /// </summary>
    public int MaxConcurrentSessions { get; init; } = 1;

    /// <summary>
    /// Whether tone mapping is supported for HDR content
    /// </summary>
    public bool SupportsToneMapping { get; init; }
}

/// <summary>
/// NVIDIA NVENC capabilities
/// </summary>
public record NvencCapabilities
{
    public bool IsAvailable { get; init; }
    public string? GpuName { get; init; }
    public string? DriverVersion { get; init; }
    public bool SupportsH264 { get; init; }
    public bool SupportsHevc { get; init; }
    public bool SupportsAv1 { get; init; }
    public bool SupportsBFrames { get; init; }
    public bool Supports10Bit { get; init; }
    public bool SupportsHdr { get; init; }
    public int MaxSessions { get; init; }
    public string[] SupportedProfiles { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Intel QuickSync Video capabilities
/// </summary>
public record QuickSyncCapabilities
{
    public bool IsAvailable { get; init; }
    public string? GpuName { get; init; }
    public string? DriverVersion { get; init; }
    public bool SupportsH264 { get; init; }
    public bool SupportsHevc { get; init; }
    public bool SupportsAv1 { get; init; }
    public bool SupportsVp9 { get; init; }
    public bool Supports10Bit { get; init; }
    public bool SupportsHdr { get; init; }
    public bool SupportsLowPower { get; init; }
    public int MaxSessions { get; init; }
    public string[] SupportedProfiles { get; init; } = Array.Empty<string>();
}

/// <summary>
/// AMD AMF capabilities
/// </summary>
public record AmfCapabilities
{
    public bool IsAvailable { get; init; }
    public string? GpuName { get; init; }
    public string? DriverVersion { get; init; }
    public bool SupportsH264 { get; init; }
    public bool SupportsHevc { get; init; }
    public bool SupportsAv1 { get; init; }
    public bool Supports10Bit { get; init; }
    public bool SupportsHdr { get; init; }
    public bool SupportsPreAnalysis { get; init; }
    public int MaxSessions { get; init; }
    public string[] SupportedProfiles { get; init; } = Array.Empty<string>();
}

/// <summary>
/// VAAPI capabilities (Linux)
/// </summary>
public record VaapiCapabilities
{
    public bool IsAvailable { get; init; }
    public string? DevicePath { get; init; }
    public string? DriverName { get; init; }
    public bool SupportsH264 { get; init; }
    public bool SupportsHevc { get; init; }
    public bool SupportsVp8 { get; init; }
    public bool SupportsVp9 { get; init; }
    public bool SupportsAv1 { get; init; }
    public bool Supports10Bit { get; init; }
    public bool SupportsHdr { get; init; }
    public int MaxSessions { get; init; }
    public string[] SupportedProfiles { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Apple VideoToolbox capabilities
/// </summary>
public record VideoToolboxCapabilities
{
    public bool IsAvailable { get; init; }
    public string? DeviceModel { get; init; }
    public bool SupportsH264 { get; init; }
    public bool SupportsHevc { get; init; }
    public bool SupportsProRes { get; init; }
    public bool Supports10Bit { get; init; }
    public bool SupportsHdr { get; init; }
    public bool SupportsAlpha { get; init; }
    public int MaxSessions { get; init; }
    public string[] SupportedProfiles { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Rockchip MPP capabilities
/// </summary>
public record RockchipCapabilities
{
    public bool IsAvailable { get; init; }
    public string? ChipModel { get; init; }
    public bool SupportsH264 { get; init; }
    public bool SupportsHevc { get; init; }
    public bool SupportsVp8 { get; init; }
    public bool SupportsVp9 { get; init; }
    public bool Supports10Bit { get; init; }
    public int MaxSessions { get; init; }
    public string[] SupportedProfiles { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Enhanced hardware acceleration methods
/// </summary>
public enum HwAccelMethod
{
    None,
    Nvenc,
    QuickSync,
    Amf,
    Vaapi,
    VideoToolbox,
    Rockchip
}