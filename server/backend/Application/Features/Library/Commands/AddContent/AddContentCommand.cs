using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using MediatR;

namespace Lanflix.Application.Features.Library.Commands.AddContent;

public class AddContentCommand : IRequest<int>
{
    public int TmdbId { get; set; }
    public ContentType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? Overview { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public MediaInfo? MediaInfo { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public double? Rating { get; set; }
    public string[]? Genres { get; set; }
}
