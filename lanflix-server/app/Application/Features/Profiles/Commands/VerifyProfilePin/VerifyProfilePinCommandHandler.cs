using Lanflix.Application.Common.Exceptions;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Features.Profiles.Commands.VerifyProfilePin;

public class VerifyProfilePinCommandHandler : IRequestHandler<VerifyProfilePinCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public VerifyProfilePinCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        VerifyProfilePinCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _context.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId, cancellationToken);

        if (profile == null)
        {
            throw new NotFoundException(nameof(Profile), request.ProfileId);
        }

        // If profile has no PIN set, verification succeeds automatically
        if (string.IsNullOrEmpty(profile.PinCode))
        {
            return true;
        }

        return profile.PinCode == request.PinCode;
    }
}
