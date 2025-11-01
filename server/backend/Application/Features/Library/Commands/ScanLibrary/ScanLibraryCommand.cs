using MediatR;

namespace Lanflix.Application.Features.Library.Commands.ScanLibrary;

public class ScanLibraryCommand : IRequest<ScanLibraryResult>
{
    public string? Path { get; set; }
    public bool FullScan { get; set; } = false;
}

public class ScanLibraryResult
{
    public int FilesScanned { get; set; }
    public int NewContentAdded { get; set; }
    public int ContentUpdated { get; set; }
    public int ContentRemoved { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Errors { get; set; } = new();
}
