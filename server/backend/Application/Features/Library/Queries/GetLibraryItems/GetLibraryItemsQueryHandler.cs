using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Features.Library.Queries.GetLibraryItems;

public class GetLibraryItemsQueryHandler : IRequestHandler<GetLibraryItemsQuery, PaginatedList<ContentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLibraryItemsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<ContentDto>> Handle(
        GetLibraryItemsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Contents.AsQueryable();

        // Filter by type
        if (request.Type.HasValue)
        {
            query = query.Where(c => c.Type == request.Type.Value);
        }

        // Filter by search term
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(searchTerm) ||
                (c.OriginalTitle != null && c.OriginalTitle.ToLower().Contains(searchTerm)));
        }

        // Filter by genre
        if (!string.IsNullOrWhiteSpace(request.Genre))
        {
            query = query.Where(c => c.Genres != null && c.Genres.Contains(request.Genre));
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "title" => request.SortDescending
                ? query.OrderByDescending(c => c.Title)
                : query.OrderBy(c => c.Title),
            "releasedate" => request.SortDescending
                ? query.OrderByDescending(c => c.ReleaseDate)
                : query.OrderBy(c => c.ReleaseDate),
            "rating" => request.SortDescending
                ? query.OrderByDescending(c => c.Rating)
                : query.OrderBy(c => c.Rating),
            _ => request.SortDescending
                ? query.OrderByDescending(c => c.AddedAt)
                : query.OrderBy(c => c.AddedAt)
        };

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
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
            .ToListAsync(cancellationToken);

        return new PaginatedList<ContentDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
