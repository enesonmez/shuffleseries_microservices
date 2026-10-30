using FluentValidation;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.Translations.RemoveMoodTranslation;

public sealed class RemoveMoodTranslationCommandValidator : AbstractValidator<RemoveMoodTranslationCommand>
{
    public RemoveMoodTranslationCommandValidator()
    {
        RuleFor(x => x.MoodId)
            .NotEmpty().WithMessage("Mood ID is required.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(LanguageCode.IsSupported).WithMessage("Language code must be a supported ISO 639-1 code (e.g. 'tr', 'en').");
    }
}
