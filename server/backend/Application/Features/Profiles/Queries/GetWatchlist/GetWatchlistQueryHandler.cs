using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Features.Profiles.Queries.GetWatchlist;

public class GetWatchlistQueryHandler : IRequestHandler<GetWatchlistQuery, List<ContentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWatchlistQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ContentDto>> Handle(
        GetWatchlistQuery request,
        CancellationToken cancellationToken)
    {
        var watchlistItems = await _context.Watchlists
            .Where(w => w.ProfileId == request.ProfileId)
            .Include(w => w.Content)
            .OrderByDescending(w => w.AddedAt)
            .Select(w => new ContentDto
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
                UpdatedAt = w.Content.UpdatedAt,
                EpisodeCount = w.Content.Episodes.Count
            })
            .ToListAsync(cancellationToken);

        return watchlistItems;
    }
}
