using FluentValidation;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Commands.CreateGenre;

public sealed class CreateGenreCommandValidator : AbstractValidator<CreateGenreCommand>
{
    public CreateGenreCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Genre code is required.")
            .MaximumLength(50).WithMessage("Genre code cannot exceed 50 characters.")
            .Matches("^[a-z0-9_-]+$").WithMessage("Genre code must contain only lowercase alphanumeric characters, dashes, or underscores.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Genre name is required.")
            .MaximumLength(100).WithMessage("Genre name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Genre description cannot exceed 1000 characters.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(LanguageCode.IsSupported).WithMessage("Language code must be a supported ISO 639-1 code (e.g. 'tr', 'en').");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to zero.");
    }
}
