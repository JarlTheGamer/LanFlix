namespace Lanflix.Application.Common.Models;

public class RadarrMovie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Path { get; set; } = string.Empty;
    public bool HasFile { get; set; }
    public bool Monitored { get; set; }
    public int TmdbId { get; set; }
    public string? ImdbId { get; set; }
    public int QualityProfileId { get; set; }
    public long SizeOnDisk { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RadarrSearchResult
{
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public int TmdbId { get; set; }
    public string? ImdbId { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
}

public class AddRadarrMovieRequest
{
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public int QualityProfileId { get; set; }
    public string RootFolderPath { get; set; } = string.Empty;
    public bool Monitored { get; set; } = true;
    public bool SearchForMovie { get; set; } = true;
}

public class RadarrQueueResponse
{
    public int TotalRecords { get; set; }
    public List<RadarrQueueItem> Records { get; set; } = new();
}

public class RadarrQueueItem
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public long Size { get; set; }
    public long Sizeleft { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TrackedDownloadStatus { get; set; }
    public string Protocol { get; set; } = string.Empty;
}

public class RadarrRootFolder
{
    public int Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public long FreeSpace { get; set; }
}

public class RadarrQualityProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
