using FluentValidation;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.Translations.AddOrUpdateMoodTranslation;

public sealed class AddOrUpdateMoodTranslationCommandValidator : AbstractValidator<AddOrUpdateMoodTranslationCommand>
{
    public AddOrUpdateMoodTranslationCommandValidator()
    {
        RuleFor(x => x.MoodId)
            .NotEmpty().WithMessage("Mood ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mood name is required.")
            .MaximumLength(100).WithMessage("Mood name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Mood description is required.")
            .MaximumLength(500).WithMessage("Mood description cannot exceed 500 characters.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(LanguageCode.IsSupported).WithMessage("Language code must be a supported ISO 639-1 code (e.g. 'tr', 'en').");

        RuleFor(x => x.VibeTags)
            .MaximumLength(300).When(x => !string.IsNullOrEmpty(x.VibeTags))
            .WithMessage("Vibe tags cannot exceed 300 characters.");
    }
}
