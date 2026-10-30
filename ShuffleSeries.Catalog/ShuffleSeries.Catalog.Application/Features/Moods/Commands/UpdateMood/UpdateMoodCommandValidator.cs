using FluentValidation;
using ShuffleSeries.Shared.Core.Application.Validators;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.UpdateMood;

public sealed class UpdateMoodCommandValidator : AbstractValidator<UpdateMoodCommand>
{
    public UpdateMoodCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mood ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mood code is required.")
            .MaximumLength(50).WithMessage("Mood code cannot exceed 50 characters.")
            .Matches("^[a-z0-9_-]+$").WithMessage("Mood code must contain only lowercase alphanumeric characters, dashes, or underscores.");

        RuleFor(x => x.AmbientColorHex)
            .NotEmpty().WithMessage("Ambient color hex is required.")
            .Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$").WithMessage("Ambient color hex must be a valid hex color code (e.g. #FF5733).");

        RuleFor(x => x.IconUrl)
            .MaximumLength(500).WithMessage("Icon URL cannot exceed 500 characters.")
            .MustBeHttpUrl();

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to zero.");
    }
}
