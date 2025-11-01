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
        // Fetch content with related entities
        var contentEntity = await _context.Contents
            .Include(c => c.Episodes)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (contentEntity == null)
        {
            throw new NotFoundException("Content", request.Id);
        }

        // Map to DTO
        var content = new ContentDto
        {
            Id = contentEntity.Id,
            TmdbId = contentEntity.TmdbId,
            Type = contentEntity.Type,
            Title = contentEntity.Title,
            OriginalTitle = contentEntity.OriginalTitle,
            Overview = contentEntity.Overview,
            FilePath = contentEntity.FilePath,
            MediaInfo = contentEntity.MediaInfo,
            ReleaseDate = contentEntity.ReleaseDate,
            PosterPath = contentEntity.PosterPath,
            BackdropPath = contentEntity.BackdropPath,
            Rating = contentEntity.Rating,
            Genres = contentEntity.Genres,
            AddedAt = contentEntity.AddedAt,
            UpdatedAt = contentEntity.UpdatedAt,
            EpisodeCount = contentEntity.Episodes?.Count ?? 0
        };

        return content;
    }
}
