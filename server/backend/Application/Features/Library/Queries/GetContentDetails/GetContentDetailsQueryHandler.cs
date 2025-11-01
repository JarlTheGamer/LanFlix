using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Exceptions;
using Lanflix.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Features.Library.Queries.GetContentDetails;

public class GetContentDetailsQueryHandler : IRequestHandler<GetContentDetailsQuery, ContentDto>
{
    private readonly IApplicationDbContext _context;

    public GetContentDetailsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ContentDto> Handle(
        GetContentDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var content = await _context.Contents
            .Include(c => c.Episodes)
            .Where(c => c.Id == request.Id)
            .Select(c => new ContentDto
            {
                Id = c.Id,
                TmdbId = c.TmdbId,
                Type = c.Type,
                Title = c.Title,
                OriginalTitle = c.OriginalTitle,
                Overview = c.Overview,
                FilePath = c.FilePath,
                MediaInfo = c.MediaInfo,
                ReleaseDate = c.ReleaseDate,
                PosterPath = c.PosterPath,
                BackdropPath = c.BackdropPath,
                Rating = c.Rating,
                Genres = c.Genres,
                AddedAt = c.AddedAt,
                UpdatedAt = c.UpdatedAt,
                EpisodeCount = c.Episodes.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (content == null)
        {
            throw new NotFoundException("Content", request.Id);
        }

        return content;
    }
}
