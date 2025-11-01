using Lanflix.Application.Common.DTOs;
using MediatR;

namespace Lanflix.Application.Features.Profiles.Queries.GetWatchHistory;

public class GetWatchHistoryQuery : IRequest<List<WatchHistoryDto>>
{
    public int ProfileId { get; set; }
    public int? Limit { get; set; } = 50;
}
