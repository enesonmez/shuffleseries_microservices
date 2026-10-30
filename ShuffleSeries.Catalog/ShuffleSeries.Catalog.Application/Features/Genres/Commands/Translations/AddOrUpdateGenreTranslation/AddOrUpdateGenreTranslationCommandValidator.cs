using FluentValidation;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Commands.Translations.AddOrUpdateGenreTranslation;

public sealed class AddOrUpdateGenreTranslationCommandValidator : AbstractValidator<AddOrUpdateGenreTranslationCommand>
{
    public AddOrUpdateGenreTranslationCommandValidator()
    {
        RuleFor(x => x.GenreId)
            .NotEmpty().WithMessage("Genre ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Genre name is required.")
            .MaximumLength(100).WithMessage("Genre name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Genre description cannot exceed 500 characters.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(LanguageCode.IsSupported).WithMessage("Language code must be a supported ISO 639-1 code (e.g. 'tr', 'en').");
    }
}
