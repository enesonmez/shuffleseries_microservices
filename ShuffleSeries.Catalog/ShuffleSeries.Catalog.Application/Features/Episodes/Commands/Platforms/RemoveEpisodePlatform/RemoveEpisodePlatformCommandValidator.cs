using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Platforms.RemoveEpisodePlatform;

public sealed class RemoveEpisodePlatformCommandValidator : AbstractValidator<RemoveEpisodePlatformCommand>
{
    public RemoveEpisodePlatformCommandValidator()
    {
        RuleFor(x => x.EpisodeId)
            .NotEmpty().WithMessage("Episode ID is required.");

        RuleFor(x => x.PlatformId)
            .NotEmpty().WithMessage("Platform ID is required.");
    }
}
