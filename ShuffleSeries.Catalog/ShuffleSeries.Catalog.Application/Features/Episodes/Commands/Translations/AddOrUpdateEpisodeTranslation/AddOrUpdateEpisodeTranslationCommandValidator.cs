using FluentValidation;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Translations.AddOrUpdateEpisodeTranslation;

public sealed class AddOrUpdateEpisodeTranslationCommandValidator : AbstractValidator<AddOrUpdateEpisodeTranslationCommand>
{
    public AddOrUpdateEpisodeTranslationCommandValidator()
    {
        RuleFor(x => x.EpisodeId)
            .NotEmpty().WithMessage("Episode ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title cannot exceed 300 characters.");

        RuleFor(x => x.Overview)
            .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Overview))
            .WithMessage("Overview cannot exceed 2000 characters.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(LanguageCode.IsSupported).WithMessage("Language code must be a supported ISO 639-1 code (e.g. 'tr', 'en').");
    }
}
