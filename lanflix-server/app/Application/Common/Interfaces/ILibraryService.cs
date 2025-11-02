namespace Lanflix.Application.Common.Interfaces;

public interface ILibraryService
{
    /// <summary>
    /// Scan media folders for new content and update the library
    /// </summary>
    Task<LibraryScanResult> ScanLibraryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Scan a specific movies folder
    /// </summary>
    Task ScanMoviesFolderAsync(string moviesPath, LibraryScanResult stats, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scan a specific series folder
    /// </summary>
    Task ScanSeriesFolderAsync(string seriesPath, LibraryScanResult stats, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up missing content from the database
    /// </summary>
    Task CleanupMissingContentAsync(LibraryScanResult stats, CancellationToken cancellationToken = default);
}

public class LibraryScanResult
{
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Removed { get; set; }
    public List<string> Errors { get; set; } = new();
}