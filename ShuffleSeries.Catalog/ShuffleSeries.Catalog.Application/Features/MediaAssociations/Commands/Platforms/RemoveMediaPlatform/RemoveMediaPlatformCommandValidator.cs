using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Platforms.RemoveMediaPlatform;

public sealed class RemoveMediaPlatformCommandValidator : AbstractValidator<RemoveMediaPlatformCommand>
{
    public RemoveMediaPlatformCommandValidator()
    {
        RuleFor(x => x.MediaId)
            .NotEmpty().WithMessage("Media ID is required.");

        RuleFor(x => x.PlatformId)
            .NotEmpty().WithMessage("Platform ID is required.");
    }
}
