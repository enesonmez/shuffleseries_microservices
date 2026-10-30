using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.Movies.Commands.DeleteMovie;

public sealed class DeleteMovieCommandValidator : AbstractValidator<DeleteMovieCommand>
{
    public DeleteMovieCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Movie ID is required.");
    }
}
