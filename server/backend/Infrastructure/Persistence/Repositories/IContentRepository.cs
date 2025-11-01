using Lanflix.Domain.Entities;
using Lanflix.Domain.Enums;

namespace Lanflix.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository interface for Content entity with performance-optimized queries
/// </summary>
public interface IContentRepository : IRepository<Content>
{
    /// <summary>
    /// Gets paginated content with minimal data for list views (optimized with Dapper)
    /// </summary>
    Task<(IEnumerable<ContentListItem> Items, int TotalCount)> GetContentListAsync(
        ContentType? type,
        int page,
        int pageSize,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets content by TMDB ID (optimized query)
    /// </summary>
    Task<Content?> GetByTmdbIdAsync(int tmdbId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recently added content (optimized query)
    /// </summary>
    Task<IEnumerable<ContentListItem>> GetRecentlyAddedAsync(
        int count,
        ContentType? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if content exists by file path
    /// </summary>
    Task<bool> ExistsByFilePathAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight DTO for content list views
/// </summary>
public class ContentListItem
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public ContentType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public double? Rating { get; set; }
    public DateTime AddedAt { get; set; }
}
