using Lanflix.Application.Common.Behaviors;
using Lanflix.Application.Common.DTOs;
using MediatR;

namespace Lanflix.Application.Features.Library.Queries.GetContentDetails;

public class GetContentDetailsQuery : IRequest<ContentDto>
{
    public int Id { get; set; }
}
