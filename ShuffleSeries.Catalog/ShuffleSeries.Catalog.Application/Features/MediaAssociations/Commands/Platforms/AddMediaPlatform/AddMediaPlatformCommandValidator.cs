using FluentValidation;
using ShuffleSeries.Shared.Core.Application.Validators;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Platforms.AddMediaPlatform;

public sealed class AddMediaPlatformCommandValidator : AbstractValidator<AddMediaPlatformCommand>
{
    public AddMediaPlatformCommandValidator()
    {
        RuleFor(x => x.MediaId)
            .NotEmpty().WithMessage("Media ID is required.");

        RuleFor(x => x.PlatformId)
            .NotEmpty().WithMessage("Platform ID is required.");

        RuleFor(x => x.WebUrl)
            .NotEmpty().WithMessage("Web URL is required.")
            .MaximumLength(500).WithMessage("Web URL cannot exceed 500 characters.")
            .MustBeHttpUrl();

        RuleFor(x => x.AppSchemeUrl)
            .MaximumLength(200).WithMessage("App scheme URL cannot exceed 200 characters.")
            .MustBeCustomAppScheme();
    }
}
