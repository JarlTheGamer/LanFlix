namespace Lanflix.Application.Common.Models;

/// <summary>
/// TMDB search result
/// </summary>
public class TmdbSearchResult
{
    public int Page { get; set; }
    public List<TmdbSearchItem> Results { get; set; } = new();
    public int TotalPages { get; set; }
    public int TotalResults { get; set; }
}

/// <summary>
/// TMDB search result item
/// </summary>
public class TmdbSearchItem
{
    private const string ImageBaseUrl = "https://image.tmdb.org/t/p";
    
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Name { get; set; }
    public string? OriginalTitle { get; set; }
    public string? OriginalName { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public DateTime? FirstAirDate { get; set; }
    public double VoteAverage { get; set; }
    public List<int> GenreIds { get; set; } = new();
    public string? MediaType { get; set; } // "movie" or "tv" from TMDB API
    
    // Computed properties for full image URLs
    public string? PosterUrl => !string.IsNullOrEmpty(PosterPath) ? $"{ImageBaseUrl}/w500{PosterPath}" : null;
    public string? BackdropUrl => !string.IsNullOrEmpty(BackdropPath) ? $"{ImageBaseUrl}/w1280{BackdropPath}" : null;
    
    // Computed property for normalized type (series instead of tv)
    public string Type => MediaType == "tv" ? "series" : (MediaType ?? "movie");
    
    // Computed property for TMDB ID
    public int TmdbId => Id;
}

/// <summary>
/// TMDB movie details
/// </summary>
public class TmdbMovieDetails
{
    private const string ImageBaseUrl = "https://image.tmdb.org/t/p";
    
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public int Runtime { get; set; }
    public double VoteAverage { get; set; }
    public List<TmdbGenre> Genres { get; set; } = new();
    public string? Tagline { get; set; }
    public string? ImdbId { get; set; }
    
    // Computed properties for full image URLs
    public string? PosterUrl => !string.IsNullOrEmpty(PosterPath) ? $"{ImageBaseUrl}/w500{PosterPath}" : null;
    public string? BackdropUrl => !string.IsNullOrEmpty(BackdropPath) ? $"{ImageBaseUrl}/w1280{BackdropPath}" : null;
    
    // Type identifier for frontend
    public string Type => "movie";
    public int TmdbId => Id;
}

/// <summary>
/// TMDB TV series details
/// </summary>
public class TmdbTvSeriesDetails
{
    private const string ImageBaseUrl = "https://image.tmdb.org/t/p";
    
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OriginalName { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public DateTime? FirstAirDate { get; set; }
    public DateTime? LastAirDate { get; set; }
    public int NumberOfSeasons { get; set; }
    public int NumberOfEpisodes { get; set; }
    public double VoteAverage { get; set; }
    public List<TmdbGenre> Genres { get; set; } = new();
    public List<TmdbSeason> Seasons { get; set; } = new();
    
    // Computed properties for full image URLs
    public string? PosterUrl => !string.IsNullOrEmpty(PosterPath) ? $"{ImageBaseUrl}/w500{PosterPath}" : null;
    public string? BackdropUrl => !string.IsNullOrEmpty(BackdropPath) ? $"{ImageBaseUrl}/w1280{BackdropPath}" : null;
    
    // Type identifier for frontend (use "series" instead of "tv" for consistency)
    public string Type => "series";
    public int TmdbId => Id;
    // Alias Name to Title for consistency with movies
    public string Title => Name;
}

/// <summary>
/// TMDB season details
/// </summary>
public class TmdbSeasonDetails
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public int SeasonNumber { get; set; }
    public DateTime? AirDate { get; set; }
    public List<TmdbEpisode> Episodes { get; set; } = new();
}

/// <summary>
/// TMDB genre
/// </summary>
public class TmdbGenre
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// TMDB season
/// </summary>
public class TmdbSeason
{
    private const string ImageBaseUrl = "https://image.tmdb.org/t/p";
    
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public int SeasonNumber { get; set; }
    public int EpisodeCount { get; set; }
    public DateTime? AirDate { get; set; }
    
    // Computed property for full image URL
    public string? PosterUrl => !string.IsNullOrEmpty(PosterPath) ? $"{ImageBaseUrl}/w500{PosterPath}" : null;
}

/// <summary>
/// TMDB episode
/// </summary>
public class TmdbEpisode
{
    private const string ImageBaseUrl = "https://image.tmdb.org/t/p";
    
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public string? StillPath { get; set; }
    public int EpisodeNumber { get; set; }
    public int SeasonNumber { get; set; }
    public DateTime? AirDate { get; set; }
    public double VoteAverage { get; set; }
    
    // Computed property for full image URL
    public string? StillUrl => !string.IsNullOrEmpty(StillPath) ? $"{ImageBaseUrl}/w300{StillPath}" : null;
}
