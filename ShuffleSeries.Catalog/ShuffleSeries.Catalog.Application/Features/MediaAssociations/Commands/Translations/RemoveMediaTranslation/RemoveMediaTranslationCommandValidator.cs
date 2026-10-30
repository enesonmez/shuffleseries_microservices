using FluentValidation;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Translations.RemoveMediaTranslation;

public sealed class RemoveMediaTranslationCommandValidator : AbstractValidator<RemoveMediaTranslationCommand>
{
    public RemoveMediaTranslationCommandValidator()
    {
        RuleFor(x => x.MediaId)
            .NotEmpty().WithMessage("Media ID is required.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(LanguageCode.IsSupported).WithMessage("Language code must be a supported ISO 639-1 code (e.g. 'tr', 'en').");
    }
}
