using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Genres.AddMediaGenre;

public sealed class AddMediaGenreCommandValidator : AbstractValidator<AddMediaGenreCommand>
{
    public AddMediaGenreCommandValidator()
    {
        RuleFor(x => x.MediaId)
            .NotEmpty().WithMessage("Media ID is required.");

        RuleFor(x => x.GenreId)
            .NotEmpty().WithMessage("Genre ID is required.");
    }
}
