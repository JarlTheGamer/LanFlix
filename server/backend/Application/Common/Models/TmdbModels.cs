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
}

/// <summary>
/// TMDB movie details
/// </summary>
public class TmdbMovieDetails
{
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
}

/// <summary>
/// TMDB TV series details
/// </summary>
public class TmdbTvSeriesDetails
{
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
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public int SeasonNumber { get; set; }
    public int EpisodeCount { get; set; }
    public DateTime? AirDate { get; set; }
}

/// <summary>
/// TMDB episode
/// </summary>
public class TmdbEpisode
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public string? StillPath { get; set; }
    public int EpisodeNumber { get; set; }
    public int SeasonNumber { get; set; }
    public DateTime? AirDate { get; set; }
    public double VoteAverage { get; set; }
}
