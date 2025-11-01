using Lanflix.Application.Common.Models;

namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Service for detecting hardware acceleration capabilities
/// </summary>
public interface IHardwareAccelerationDetector
{
    /// <summary>
    /// Detects available hardware acceleration methods
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Hardware acceleration capabilities</returns>
    Task<HwAccelCapabilities> DetectAsync(CancellationToken cancellationToken = default);
}
