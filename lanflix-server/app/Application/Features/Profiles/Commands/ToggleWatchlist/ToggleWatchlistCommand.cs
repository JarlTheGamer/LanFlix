using MediatR;

namespace Lanflix.Application.Features.Profiles.Commands.ToggleWatchlist;

public class ToggleWatchlistCommand : IRequest<bool>
{
    public int ProfileId { get; set; }
    public int ContentId { get; set; }
}
