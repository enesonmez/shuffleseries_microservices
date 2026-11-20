using FluentValidation;
using ShuffleSeries.Shared.Core.Application.Validators;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Platforms.AddEpisodePlatform;

public sealed class AddEpisodePlatformCommandValidator : AbstractValidator<AddEpisodePlatformCommand>
{
    public AddEpisodePlatformCommandValidator()
    {
        RuleFor(x => x.EpisodeId)
            .NotEmpty().WithMessage("EpisodeId cannot be empty.");

        RuleFor(x => x.PlatformId)
            .NotEmpty().WithMessage("PlatformId cannot be empty.");

        RuleFor(x => x.WebUrl)
            .NotEmpty().WithMessage("WebUrl is required.")
            .MaximumLength(1000).WithMessage("WebUrl cannot exceed 1000 characters.")
            .MustBeHttpUrl();

        RuleFor(x => x.AppSchemeUrl)
            .MaximumLength(500).WithMessage("AppSchemeUrl cannot exceed 500 characters.")
            .MustBeCustomAppScheme();
    }
}
