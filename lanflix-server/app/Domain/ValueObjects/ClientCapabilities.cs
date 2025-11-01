namespace Lanflix.Domain.ValueObjects;

public record ClientCapabilities
{
    public string[] SupportedVideoCodecs { get; init; } = Array.Empty<string>();
    public string[] SupportedAudioCodecs { get; init; } = Array.Empty<string>();
    public string[] SupportedContainers { get; init; } = Array.Empty<string>();
    public int MaxBitrate { get; init; }
    public VideoResolution MaxResolution { get; init; } = VideoResolution.HD1080p;
    public bool SupportsHDR { get; init; }
}

public enum VideoResolution
{
    SD480p,
    HD720p,
    HD1080p,
    UHD4K,
    UHD8K
}
