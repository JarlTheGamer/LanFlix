namespace Lanflix.Application.Common.Models;

public class SonarrSeries
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Path { get; set; } = string.Empty;
    public bool Monitored { get; set; }
    public int TvdbId { get; set; }
    public string? ImdbId { get; set; }
    public int QualityProfileId { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<SonarrSeason> Seasons { get; set; } = new();
}

public class SonarrSeason
{
    public int SeasonNumber { get; set; }
    public bool Monitored { get; set; }
}

public class SonarrSearchResult
{
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public int TvdbId { get; set; }
    public string? ImdbId { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public List<SonarrSeason> Seasons { get; set; } = new();
}

public class AddSonarrSeriesRequest
{
    public int TvdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int QualityProfileId { get; set; }
    public string RootFolderPath { get; set; } = string.Empty;
    public bool Monitored { get; set; } = true;
    public bool SearchForMissingEpisodes { get; set; } = true;
}

public class SonarrQueueResponse
{
    public int TotalRecords { get; set; }
    public List<SonarrQueueItem> Records { get; set; } = new();
}

public class SonarrQueueItem
{
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public int EpisodeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public long Size { get; set; }
    public long Sizeleft { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TrackedDownloadStatus { get; set; }
    public string Protocol { get; set; } = string.Empty;
}

public class SonarrRootFolder
{
    public int Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public long FreeSpace { get; set; }
}

public class SonarrQualityProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SonarrEpisode
{
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public int SeasonNumber { get; set; }
    public int EpisodeNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool Monitored { get; set; }
    public bool HasFile { get; set; }
}
