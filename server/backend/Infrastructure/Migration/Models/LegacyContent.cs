namespace Lanflix.Infrastructure.Migration.Models;

/// <summary>
/// Represents a Content record from the legacy Node.js backend database
/// </summary>
public class LegacyContent
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public string Type { get; set; } = string.Empty; // 'movie' or 'series'
    public string Title { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? Overview { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public decimal? VoteAverage { get; set; }
    public int? VoteCount { get; set; }
    public string? Genres { get; set; }
    public int? Runtime { get; set; }
    public string? Status { get; set; }
    public string? FilePath { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
