using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Moods.AddMediaMood;

public sealed class AddMediaMoodCommandValidator : AbstractValidator<AddMediaMoodCommand>
{
    public AddMediaMoodCommandValidator()
    {
        RuleFor(x => x.MediaId)
            .NotEmpty().WithMessage("Media ID is required.");

        RuleFor(x => x.MoodId)
            .NotEmpty().WithMessage("Mood ID is required.");
    }
}
