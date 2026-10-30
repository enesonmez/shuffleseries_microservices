using FluentValidation;
using ShuffleSeries.Shared.Core.Application.Validators;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.CreateEpisode;

public sealed class CreateEpisodeCommandValidator : AbstractValidator<CreateEpisodeCommand>
{
    public CreateEpisodeCommandValidator()
    {
        RuleFor(x => x.SeriesId)
            .NotEmpty().WithMessage("Series ID is required.");

        RuleFor(x => x.SeasonNumber)
            .GreaterThan(0).WithMessage("Season number must be greater than zero.");

        RuleFor(x => x.EpisodeNumber)
            .GreaterThan(0).WithMessage("Episode number must be greater than zero.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(250).WithMessage("Title cannot exceed 250 characters.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(LanguageCode.IsSupported).WithMessage("Language code must be a supported ISO 639-1 code (e.g. 'tr', 'en').");

        RuleFor(x => x.RuntimeMinutes)
            .GreaterThan(0).When(x => x.RuntimeMinutes.HasValue)
            .WithMessage("Runtime minutes must be greater than zero.");

        RuleFor(x => x.ImdbRating)
            .InclusiveBetween(0.0, 10.0).When(x => x.ImdbRating.HasValue)
            .WithMessage("IMDb rating must be between 0.0 and 10.0.");

        RuleFor(x => x.ThumbnailUrl)
            .MaximumLength(500).WithMessage("Thumbnail URL cannot exceed 500 characters.")
            .MustBeHttpUrl();
    }
}
