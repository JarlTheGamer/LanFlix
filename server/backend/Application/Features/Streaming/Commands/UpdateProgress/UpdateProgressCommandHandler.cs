using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Features.Streaming.Commands.UpdateProgress;

public class UpdateProgressCommandHandler : IRequestHandler<UpdateProgressCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public UpdateProgressCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(
        UpdateProgressCommand request,
        CancellationToken cancellationToken)
    {
        // Find existing watch history
        var watchHistory = await _context.WatchHistories
            .FirstOrDefaultAsync(w =>
                w.ProfileId == request.ProfileId &&
                w.ContentId == request.ContentId &&
                w.EpisodeId == request.EpisodeId,
                cancellationToken);

        if (watchHistory == null)
        {
            // Create new watch history
            watchHistory = new WatchHistory
            {
                ProfileId = request.ProfileId,
                ContentId = request.ContentId,
                EpisodeId = request.EpisodeId,
                PositionTicks = request.PositionTicks,
                IsCompleted = request.IsCompleted,
                LastWatchedAt = DateTime.UtcNow
            };

            _context.WatchHistories.Add(watchHistory);
        }
        else
        {
            // Update existing watch history
            watchHistory.PositionTicks = request.PositionTicks;
            watchHistory.IsCompleted = request.IsCompleted;
            watchHistory.LastWatchedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
