using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Moods.RemoveMediaMood;

public sealed class RemoveMediaMoodCommandValidator : AbstractValidator<RemoveMediaMoodCommand>
{
    public RemoveMediaMoodCommandValidator()
    {
        RuleFor(x => x.MediaId)
            .NotEmpty().WithMessage("Media ID is required.");

        RuleFor(x => x.MoodId)
            .NotEmpty().WithMessage("Mood ID is required.");
    }
}
