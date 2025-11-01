namespace Lanflix.Application.Common.Models;

/// <summary>
/// Represents hardware acceleration capabilities detected on the system
/// </summary>
public class HwAccelCapabilities
{
    /// <summary>
    /// NVIDIA NVENC is available
    /// </summary>
    public bool HasNvenc { get; set; }

    /// <summary>
    /// Intel QuickSync is available
    /// </summary>
    public bool HasQuickSync { get; set; }

    /// <summary>
    /// AMD AMF is available
    /// </summary>
    public bool HasAmf { get; set; }

    /// <summary>
    /// VAAPI (Linux) is available
    /// </summary>
    public bool HasVaapi { get; set; }

    /// <summary>
    /// VideoToolbox (macOS) is available
    /// </summary>
    public bool HasVideoToolbox { get; set; }

    /// <summary>
    /// Preferred hardware acceleration method
    /// </summary>
    public HwAccelMethod PreferredMethod { get; set; } = HwAccelMethod.None;

    /// <summary>
    /// Indicates if any hardware acceleration is available
    /// </summary>
    public bool IsAvailable => PreferredMethod != HwAccelMethod.None;
}

/// <summary>
/// Hardware acceleration methods
/// </summary>
public enum HwAccelMethod
{
    None,
    Nvenc,
    QuickSync,
    Amf,
    Vaapi,
    VideoToolbox
}
