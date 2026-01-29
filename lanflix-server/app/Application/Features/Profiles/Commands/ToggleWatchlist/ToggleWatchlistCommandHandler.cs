using Lanflix.Application.Common.Exceptions;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lanflix.Application.Features.Profiles.Commands.ToggleWatchlist;

public class ToggleWatchlistCommandHandler : IRequestHandler<ToggleWatchlistCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ToggleWatchlistCommandHandler> _logger;

    public ToggleWatchlistCommandHandler(IApplicationDbContext context, ILogger<ToggleWatchlistCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(ToggleWatchlistCommand request, CancellationToken cancellationToken)
    {
        // Validate Profile exists
        var profile = await _context.Profiles
            .FindAsync(new object[] { request.ProfileId }, cancellationToken);

        if (profile == null)
        {
            throw new NotFoundException(nameof(Profile), request.ProfileId);
        }

        // Validate Content exists
        var content = await _context.Contents
            .FindAsync(new object[] { request.ContentId }, cancellationToken);
            
        // If content is not found in local DB, we might want to allow adding it if we can fetch it from TMDB.
        // But for now let's assume content must exist in library (or we should throw NotFound).
        // The error log showed contentId 1084242, which looks like a TMDB ID.
        // It's possible the frontend is sending TMDB ID but we expect Content.Id (our internal ID).
        // Let's check Content entity to see if Id matches. Usually internal IDs are small integers unless auto-incrementing.
        // If the frontend is navigating a library, it likely has the internal ID.
        
        if (content == null)
        {
            // Fallback: Check if request.ContentId is actually a TmdbId? 
            // The frontend usually deals with whatever the API returns.
            // If the user was viewing details of a TMDB item not in library, they can't add it to watchlist
            // unless we support watchlisting un-added items (which requires adding them to Contents first).
            // For now, let's assume strict existing content check.
            throw new NotFoundException(nameof(Content), request.ContentId);
        }

        var existingItem = await _context.Watchlists
            .FirstOrDefaultAsync(w => w.ProfileId == request.ProfileId && w.ContentId == request.ContentId, cancellationToken);

        if (existingItem != null)
        {
            // Remove from watchlist
            _context.Watchlists.Remove(existingItem);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed content {ContentId} from profile {ProfileId} watchlist", request.ContentId, request.ProfileId);
            return false; // Removed
        }
        else
        {
            // Add to watchlist
            var watchlistItem = new Watchlist
            {
                ProfileId = request.ProfileId,
                ContentId = request.ContentId,
                AddedAt = DateTime.UtcNow
            };

            _context.Watchlists.Add(watchlistItem);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Added content {ContentId} to profile {ProfileId} watchlist", request.ContentId, request.ProfileId);
            return true; // Added
        }
    }
}
