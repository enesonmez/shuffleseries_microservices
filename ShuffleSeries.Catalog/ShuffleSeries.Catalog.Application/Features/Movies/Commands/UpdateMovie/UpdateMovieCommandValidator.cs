using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.Movies.Commands.UpdateMovie;

public sealed class UpdateMovieCommandValidator : AbstractValidator<UpdateMovieCommand>
{
    public UpdateMovieCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Movie ID is required.");

        RuleFor(x => x.TmdbId)
            .GreaterThan(0).WithMessage("TMDb ID must be greater than zero.");

        RuleFor(x => x.ImdbId)
            .NotEmpty().WithMessage("IMDb ID is required.")
            .Matches(@"^tt\d{7,10}$").WithMessage("IMDb ID must follow the standard format (e.g. 'tt0111161').");

        RuleFor(x => x.ReleaseYear)
            .InclusiveBetween(1888, 2100).WithMessage("Release year must be between 1888 and 2100.");

        RuleFor(x => x.RuntimeMinutes)
            .GreaterThan(0).WithMessage("Runtime minutes must be greater than zero.");

        RuleFor(x => x.ImdbRating)
            .InclusiveBetween(0.0, 10.0).When(x => x.ImdbRating.HasValue)
            .WithMessage("IMDb rating must be between 0.0 and 10.0.");

        RuleFor(x => x.ImdbVotes)
            .GreaterThanOrEqualTo(0).When(x => x.ImdbVotes.HasValue)
            .WithMessage("IMDb votes cannot be negative.");
    }
}
