using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.DeleteMood;

public sealed class DeleteMoodCommandValidator : AbstractValidator<DeleteMoodCommand>
{
    public DeleteMoodCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mood ID is required.");
    }
}
