namespace Lanflix.Application.Common.DTOs;

public class ServerSettingsDto
{
    public MediaPathsSettings MediaPaths { get; set; } = new();
    public TranscodingSettings Transcoding { get; set; } = new();
    public StreamingSettings Streaming { get; set; } = new();
    public CacheSettings Cache { get; set; } = new();
    public ExternalApisSettings ExternalApis { get; set; } = new();
}

public class MediaPathsSettings
{
    public string Movies { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public string PosterCache { get; set; } = string.Empty;
    public string BackdropCache { get; set; } = string.Empty;
}

public class TranscodingSettings
{
    public bool EnableHardwareAcceleration { get; set; }
    public string PreferredHwAccel { get; set; } = "auto";
    public int MaxConcurrentTranscodes { get; set; }
    public string TempPath { get; set; } = string.Empty;
    public int DefaultBitrate { get; set; }
    public int HlsSegmentDuration { get; set; }
}

public class StreamingSettings
{
    public bool EnableDirectPlay { get; set; }
    public bool EnableDirectStream { get; set; }
    public int ChunkSize { get; set; }
}

public class CacheSettings
{
    public RedisCacheSettings Redis { get; set; } = new();
    public MemoryCacheSettings Memory { get; set; } = new();
}

public class RedisCacheSettings
{
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
}

public class MemoryCacheSettings
{
    public int SizeLimit { get; set; }
}

public class ExternalApisSettings
{
    public TmdbSettings Tmdb { get; set; } = new();
    public ExternalServiceSettings Sonarr { get; set; } = new();
    public ExternalServiceSettings Radarr { get; set; } = new();
    public ExternalServiceSettings Prowlarr { get; set; } = new();
    public SubtitleSettings Subtitles { get; set; } = new();
}

public class TmdbSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}

public class ExternalServiceSettings
{
    public string Url { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class SubtitleSettings
{
    /// <summary>
    /// Preferred subtitle language for automatic downloads (ISO 639-2 code, e.g., "eng", "spa", "fra")
    /// </summary>
    public string PreferredLanguage { get; set; } = "eng";
    
    /// <summary>
    /// Whether to automatically download subtitles when downloading content
    /// </summary>
    public bool AutoDownload { get; set; } = true;
}
