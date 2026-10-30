using FluentValidation;
using ShuffleSeries.Shared.Core.Application.Validators;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Features.Movies.Commands.CreateMovie;

public sealed class CreateMovieCommandValidator : AbstractValidator<CreateMovieCommand>
{
    public CreateMovieCommandValidator()
    {
        RuleFor(x => x.TmdbId)
            .GreaterThan(0).WithMessage("TMDb ID must be greater than zero.");

        RuleFor(x => x.ImdbId)
            .NotEmpty().WithMessage("IMDb ID is required.")
            .Matches(@"^tt\d{7,10}$").WithMessage("IMDb ID must follow the standard format (e.g. 'tt0111161').");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(250).WithMessage("Title cannot exceed 250 characters.");

        RuleFor(x => x.Overview)
            .NotEmpty().WithMessage("Overview is required.")
            .MaximumLength(4000).WithMessage("Overview cannot exceed 4000 characters.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(LanguageCode.IsSupported).WithMessage("Language code must be a supported ISO 639-1 code (e.g. 'tr', 'en').");

        RuleFor(x => x.PosterUrl)
            .NotEmpty().WithMessage("Poster URL is required.")
            .MaximumLength(1000).WithMessage("Poster URL cannot exceed 1000 characters.")
            .MustBeHttpUrl();

        RuleFor(x => x.BackdropUrl)
            .MaximumLength(1000).WithMessage("Backdrop URL cannot exceed 1000 characters.")
            .MustBeHttpUrl();

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
