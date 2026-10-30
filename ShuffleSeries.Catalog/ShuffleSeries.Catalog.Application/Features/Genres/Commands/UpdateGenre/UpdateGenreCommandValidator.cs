using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Commands.UpdateGenre;

public sealed class UpdateGenreCommandValidator : AbstractValidator<UpdateGenreCommand>
{
    public UpdateGenreCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Genre ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Genre code is required.")
            .MaximumLength(50).WithMessage("Genre code cannot exceed 50 characters.")
            .Matches("^[a-z0-9_-]+$").WithMessage("Genre code must contain only lowercase alphanumeric characters, dashes, or underscores.");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to zero.");
    }
}
