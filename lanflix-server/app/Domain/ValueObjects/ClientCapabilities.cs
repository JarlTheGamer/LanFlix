namespace Lanflix.Domain.ValueObjects;

public class ClientCapabilities
{
    public string[] SupportedVideoCodecs { get; set; } = Array.Empty<string>();
    public string[] SupportedAudioCodecs { get; set; } = Array.Empty<string>();
    public string[] SupportedContainers { get; set; } = Array.Empty<string>();
    public int MaxBitrate { get; set; }
    public VideoResolution MaxResolution { get; set; }
    public bool SupportsHDR { get; set; }
}