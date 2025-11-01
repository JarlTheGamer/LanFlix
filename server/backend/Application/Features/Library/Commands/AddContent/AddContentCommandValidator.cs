using FluentValidation;

namespace Lanflix.Application.Features.Library.Commands.AddContent;

public class AddContentCommandValidator : AbstractValidator<AddContentCommand>
{
    public AddContentCommandValidator()
    {
        RuleFor(x => x.TmdbId)
            .GreaterThan(0)
            .WithMessage("TMDB ID must be positive");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(500)
            .WithMessage("Title must not exceed 500 characters");

        RuleFor(x => x.FilePath)
            .NotEmpty()
            .WithMessage("File path is required")
            .MaximumLength(1000)
            .WithMessage("File path must not exceed 1000 characters")
            .Must(BeValidFilePath)
            .WithMessage("Invalid file path");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid content type");
    }

    private bool BeValidFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            // Check if path is fully qualified
            if (!Path.IsPathFullyQualified(path))
                return false;

            // Prevent directory traversal
            if (path.Contains(".."))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }
}
