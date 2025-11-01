using System.Diagnostics;

namespace Lanflix.Infrastructure.Telemetry;

/// <summary>
/// Provides ActivitySource instances for distributed tracing in Lanflix
/// </summary>
public static class LanflixActivitySource
{
    /// <summary>
    /// Activity source for streaming operations
    /// </summary>
    public static readonly ActivitySource Streaming = new("Lanflix.Streaming", "1.0.0");
    
    /// <summary>
    /// Activity source for transcoding operations
    /// </summary>
    public static readonly ActivitySource Transcoding = new("Lanflix.Transcoding", "1.0.0");
    
    /// <summary>
    /// Activity source for library operations
    /// </summary>
    public static readonly ActivitySource Library = new("Lanflix.Library", "1.0.0");
}
