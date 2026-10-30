using FluentValidation;
using ShuffleSeries.Shared.Core.Application.Validators;

namespace ShuffleSeries.Catalog.Application.Features.Platforms.Commands.UpdatePlatform;

public sealed class UpdatePlatformCommandValidator : AbstractValidator<UpdatePlatformCommand>
{
    public UpdatePlatformCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Platform ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Platform name is required.")
            .MaximumLength(100).WithMessage("Platform name cannot exceed 100 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Platform slug is required.")
            .MaximumLength(50).WithMessage("Platform slug cannot exceed 50 characters.")
            .Matches("^[a-z0-9-]+$").WithMessage("Platform slug must contain only lowercase alphanumeric characters and hyphens.");

        RuleFor(x => x.LogoUrl)
            .NotEmpty().WithMessage("Logo URL is required.")
            .MaximumLength(500).WithMessage("Logo URL cannot exceed 500 characters.")
            .MustBeHttpUrl();

        RuleFor(x => x.InactiveLogoUrl)
            .NotEmpty().WithMessage("Inactive logo URL is required.")
            .MaximumLength(500).WithMessage("Inactive logo URL cannot exceed 500 characters.")
            .MustBeHttpUrl();

        RuleFor(x => x.BaseWebUrl)
            .NotEmpty().WithMessage("Base web URL is required.")
            .MaximumLength(500).WithMessage("Base web URL cannot exceed 500 characters.")
            .MustBeHttpUrl();

        RuleFor(x => x.BaseAppSchemeUrl)
            .MaximumLength(200).WithMessage("Base app scheme URL cannot exceed 200 characters.")
            .MustBeCustomAppScheme();

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to zero.");
    }
}
