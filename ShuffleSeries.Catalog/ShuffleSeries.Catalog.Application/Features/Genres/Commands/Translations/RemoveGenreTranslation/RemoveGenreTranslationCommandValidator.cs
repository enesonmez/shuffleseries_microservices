using FluentValidation;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Commands.Translations.RemoveGenreTranslation;

public sealed class RemoveGenreTranslationCommandValidator : AbstractValidator<RemoveGenreTranslationCommand>
{
    public RemoveGenreTranslationCommandValidator()
    {
        RuleFor(x => x.GenreId)
            .NotEmpty().WithMessage("Genre ID is required.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(LanguageCode.IsSupported).WithMessage("Language code must be a supported ISO 639-1 code (e.g. 'tr', 'en').");
    }
}
