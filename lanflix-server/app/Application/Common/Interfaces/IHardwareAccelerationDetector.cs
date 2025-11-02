using Lanflix.Domain.ValueObjects;

namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Service for detecting hardware acceleration capabilities for modern transcoding
/// Supports Intel, AMD, Nvidia, Apple, and Rockchip GPUs
/// </summary>
public interface IHardwareAccelerationDetector
{
    /// <summary>
    /// Detects available hardware acceleration methods and capabilities
    /// </summary>
    /// <returns>Enhanced hardware acceleration capabilities</returns>
    Task<HardwareAcceleration> DetectAsync();
}
