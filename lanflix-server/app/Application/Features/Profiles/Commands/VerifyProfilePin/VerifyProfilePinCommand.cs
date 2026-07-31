using MediatR;

namespace Lanflix.Application.Features.Profiles.Commands.VerifyProfilePin;

public class VerifyProfilePinCommand : IRequest<bool>
{
    public int ProfileId { get; set; }
    public string PinCode { get; set; } = string.Empty;
}
