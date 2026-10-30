using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Genres.RemoveMediaGenre;

public sealed class RemoveMediaGenreCommandValidator : AbstractValidator<RemoveMediaGenreCommand>
{
    public RemoveMediaGenreCommandValidator()
    {
        RuleFor(x => x.MediaId)
            .NotEmpty().WithMessage("Media ID is required.");

        RuleFor(x => x.GenreId)
            .NotEmpty().WithMessage("Genre ID is required.");
    }
}
