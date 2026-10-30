using FluentValidation;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Translations.AddOrUpdateMediaTranslation;

public sealed class AddOrUpdateMediaTranslationCommandValidator : AbstractValidator<AddOrUpdateMediaTranslationCommand>
{
    public AddOrUpdateMediaTranslationCommandValidator()
    {
        RuleFor(x => x.MediaId)
            .NotEmpty().WithMessage("Media ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(250).WithMessage("Title cannot exceed 250 characters.");

        RuleFor(x => x.Overview)
            .NotEmpty().WithMessage("Overview is required.")
            .MaximumLength(4000).WithMessage("Overview cannot exceed 4000 characters.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(LanguageCode.IsSupported).WithMessage("Language code must be a supported ISO 639-1 code (e.g. 'tr', 'en').");

        RuleFor(x => x.Tagline)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Tagline))
            .WithMessage("Tagline cannot exceed 500 characters.");
    }
}
