using FluentValidation;

namespace Lanflix.Application.Features.Profiles.Commands.CreateProfile;

public class CreateProfileCommandValidator : AbstractValidator<CreateProfileCommand>
{
    public CreateProfileCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Profile name is required")
            .MaximumLength(100)
            .WithMessage("Profile name must not exceed 100 characters");

        RuleFor(x => x.AvatarPath)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.AvatarPath))
            .WithMessage("Avatar path must not exceed 500 characters");
    }
}
