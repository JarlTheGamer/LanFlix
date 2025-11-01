using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Features.Profiles.Queries.GetWatchHistory;

public class GetWatchHistoryQueryHandler : IRequestHandler<GetWatchHistoryQuery, List<WatchHistoryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWatchHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WatchHistoryDto>> Handle(
        GetWatchHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.WatchHistories
            .Include(w => w.Content)
            .Where(w => w.ProfileId == request.ProfileId)
            .OrderByDescending(w => w.LastWatchedAt);

        if (request.Limit.HasValue)
        {
            query = (IOrderedQueryable<Lanflix.Domain.Entities.WatchHistory>)query.Take(request.Limit.Value);
        }

        return await query
            .Select(w => new WatchHistoryDto
            {
                Id = w.Id,
                ProfileId = w.ProfileId,
                ContentId = w.ContentId,
                EpisodeId = w.EpisodeId,
                PositionTicks = w.PositionTicks,
                IsCompleted = w.IsCompleted,
                LastWatchedAt = w.LastWatchedAt,
                Content = new ContentDto
                {
                    Id = w.Content.Id,
                    TmdbId = w.Content.TmdbId,
                    Type = w.Content.Type,
                    Title = w.Content.Title,
                    OriginalTitle = w.Content.OriginalTitle,
                    Overview = w.Content.Overview,
                    FilePath = w.Content.FilePath,
                    MediaInfo = w.Content.MediaInfo,
                    ReleaseDate = w.Content.ReleaseDate,
                    PosterPath = w.Content.PosterPath,
                    BackdropPath = w.Content.BackdropPath,
                    Rating = w.Content.Rating,
                    Genres = w.Content.Genres,
                    AddedAt = w.Content.AddedAt,
                    UpdatedAt = w.Content.UpdatedAt
                }
            })
            .ToListAsync(cancellationToken);
    }
}
