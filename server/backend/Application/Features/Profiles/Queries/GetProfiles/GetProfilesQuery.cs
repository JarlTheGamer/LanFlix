using Lanflix.Application.Common.DTOs;
using MediatR;

namespace Lanflix.Application.Features.Profiles.Queries.GetProfiles;

public class GetProfilesQuery : IRequest<List<ProfileDto>>
{
}
