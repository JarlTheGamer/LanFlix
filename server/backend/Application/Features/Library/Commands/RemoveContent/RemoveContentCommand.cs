using MediatR;

namespace Lanflix.Application.Features.Library.Commands.RemoveContent;

public class RemoveContentCommand : IRequest<Unit>
{
    public int Id { get; set; }
}
