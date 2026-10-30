using FluentValidation;

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
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out var parsed) &&
                         (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
            .WithMessage("WebUrl must be a valid HTTP or HTTPS URL.");

        RuleFor(x => x.AppSchemeUrl)
            .MaximumLength(500).WithMessage("AppSchemeUrl cannot exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.AppSchemeUrl));
    }
}
