namespace Lanflix.Application.Common.Models;

public class ProwlarrSearchResult
{
    public string Guid { get; set; } = string.Empty;
    public int IndexerId { get; set; }
    public string Indexer { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime PublishDate { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string? InfoUrl { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public int? Seeders { get; set; }
    public int? Leechers { get; set; }
    public string? ImdbId { get; set; }
    public int? TmdbId { get; set; }
    public int? TvdbId { get; set; }
    public List<ProwlarrCategory> Categories { get; set; } = new();
}

public class ProwlarrCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProwlarrIndexer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enable { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class ProwlarrHealthCheck
{
    public string Source { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
