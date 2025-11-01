using Lanflix.Application.Common.Behaviors;
using Lanflix.Application.Common.DTOs;
using Lanflix.Domain.Enums;
using MediatR;

namespace Lanflix.Application.Features.Library.Queries.GetLibraryItems;

public class GetLibraryItemsQuery : IRequest<PaginatedList<ContentDto>>, ICacheableQuery
{
    public ContentType? Type { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? Genre { get; set; }
    public string? SortBy { get; set; } = "AddedAt";
    public bool SortDescending { get; set; } = true;

    public string CacheKey => 
        $"library:items:{Type}:{PageNumber}:{PageSize}:{SearchTerm}:{Genre}:{SortBy}:{SortDescending}";
    
    public TimeSpan? CacheExpiration => TimeSpan.FromMinutes(10);
}
