using FluentValidation;
using ShuffleSeries.Shared.Core.Application.Validators;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.UpdateEpisode;

public sealed class UpdateEpisodeCommandValidator : AbstractValidator<UpdateEpisodeCommand>
{
    public UpdateEpisodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Episode ID is required.");

        RuleFor(x => x.EpisodeNumber)
            .GreaterThan(0).WithMessage("Episode number must be greater than zero.");

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
