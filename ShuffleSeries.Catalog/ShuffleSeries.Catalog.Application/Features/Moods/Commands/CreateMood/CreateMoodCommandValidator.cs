using FluentValidation;
using ShuffleSeries.Shared.Core.Application.Validators;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.CreateMood;

public sealed class CreateMoodCommandValidator : AbstractValidator<CreateMoodCommand>
{
    public CreateMoodCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mood code is required.")
            .MaximumLength(50).WithMessage("Mood code cannot exceed 50 characters.")
            .Matches("^[a-z0-9_-]+$").WithMessage("Mood code must contain only lowercase alphanumeric characters, dashes, or underscores.");

        RuleFor(x => x.AmbientColorHex)
            .NotEmpty().WithMessage("Ambient color hex is required.")
            .Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$").WithMessage("Ambient color hex must be a valid hex color code (e.g. #FF5733).");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mood name is required.")
            .MaximumLength(100).WithMessage("Mood name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Mood description is required.")
            .MaximumLength(1000).WithMessage("Mood description cannot exceed 1000 characters.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(LanguageCode.IsSupported).WithMessage("Language code must be a supported ISO 639-1 code (e.g. 'tr', 'en').");

        RuleFor(x => x.IconUrl)
            .MaximumLength(500).WithMessage("Icon URL cannot exceed 500 characters.")
            .MustBeHttpUrl();

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to zero.");
    }
}
