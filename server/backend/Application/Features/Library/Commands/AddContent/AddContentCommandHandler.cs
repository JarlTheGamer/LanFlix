using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Features.Library.Commands.AddContent;

public class AddContentCommandHandler : IRequestHandler<AddContentCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly IProgressBroadcaster _progressBroadcaster;

    public AddContentCommandHandler(
        IApplicationDbContext context,
        ICacheService cacheService,
        IProgressBroadcaster progressBroadcaster)
    {
        _context = context;
        _cacheService = cacheService;
        _progressBroadcaster = progressBroadcaster;
    }

    public async Task<int> Handle(
        AddContentCommand request,
        CancellationToken cancellationToken)
    {
        // Check if content already exists
        var existingContent = await _context.Contents
            .FirstOrDefaultAsync(c => c.TmdbId == request.TmdbId, cancellationToken);

        if (existingContent != null)
        {
            // Update existing content
            existingContent.Title = request.Title;
            existingContent.OriginalTitle = request.OriginalTitle;
            existingContent.Overview = request.Overview;
            existingContent.FilePath = request.FilePath;
            existingContent.MediaInfo = request.MediaInfo;
            existingContent.ReleaseDate = request.ReleaseDate;
            existingContent.PosterPath = request.PosterPath;
            existingContent.BackdropPath = request.BackdropPath;
            existingContent.Rating = request.Rating;
            existingContent.Genres = request.Genres;
            existingContent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            await _cacheService.RemoveByTagAsync("library", cancellationToken);
            await _cacheService.RemoveAsync($"content:{existingContent.Id}", cancellationToken);

            return existingContent.Id;
        }

        // Create new content
        var content = new Content
        {
            TmdbId = request.TmdbId,
            Type = request.Type,
            Title = request.Title,
            OriginalTitle = request.OriginalTitle,
            Overview = request.Overview,
            FilePath = request.FilePath,
            MediaInfo = request.MediaInfo,
            ReleaseDate = request.ReleaseDate,
            PosterPath = request.PosterPath,
            BackdropPath = request.BackdropPath,
            Rating = request.Rating,
            Genres = request.Genres,
            AddedAt = DateTime.UtcNow
        };

        _context.Contents.Add(content);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveByTagAsync("library", cancellationToken);

        // Broadcast new content notification
        await _progressBroadcaster.BroadcastNewContentAsync(
            content.Id,
            content.Title,
            content.Type.ToString(),
            cancellationToken);

        return content.Id;
    }
}
