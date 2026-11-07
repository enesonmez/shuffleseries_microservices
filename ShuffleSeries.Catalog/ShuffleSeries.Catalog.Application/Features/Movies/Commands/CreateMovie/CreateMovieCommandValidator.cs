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
            .MustBeImdbId().WithMessage("IMDb ID must follow the standard format (e.g. 'tt0111161').");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(250).WithMessage("Title cannot exceed 250 characters.");

        RuleFor(x => x.Overview)
            .NotEmpty().WithMessage("Overview is required.")
            .MaximumLength(2000).WithMessage("Overview cannot exceed 2000 characters.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(LanguageCode.IsSupported).WithMessage("Language code must be a supported ISO 639-1 code (e.g. 'tr', 'en').");

        RuleFor(x => x.PosterUrl)
            .NotEmpty().WithMessage("Poster URL is required.")
            .MaximumLength(500).WithMessage("Poster URL cannot exceed 500 characters.")
            .MustBeHttpUrl();

        RuleFor(x => x.BackdropUrl)
            .MaximumLength(500).WithMessage("Backdrop URL cannot exceed 500 characters.")
            .MustBeHttpUrl();

        RuleFor(x => x.ReleaseYear)
            .InclusiveBetween(1888, 2100).WithMessage("Release year must be between 1888 and 2100.");

        RuleFor(x => x.RuntimeMinutes)
            .GreaterThan(0).WithMessage("Runtime minutes must be greater than zero.");

        RuleFor(x => x.Tagline)
            .MaximumLength(500).WithMessage("Tagline cannot exceed 500 characters.");

        RuleFor(x => x.Director)
            .MaximumLength(150).WithMessage("Director cannot exceed 150 characters.");

        RuleFor(x => x.ImdbRating)
            .InclusiveBetween(0.0, 10.0).When(x => x.ImdbRating.HasValue)
            .WithMessage("IMDb rating must be between 0.0 and 10.0.");

        RuleFor(x => x.ImdbVotes)
            .GreaterThanOrEqualTo(0).When(x => x.ImdbVotes.HasValue)
            .WithMessage("IMDb votes cannot be negative.");

        RuleFor(x => x.PlatformIds)
            .Must(ids => ids == null || ids.Count <= 50)
            .WithMessage("Platform IDs cannot exceed 50 items.");

        RuleForEach(x => x.PlatformIds)
            .NotEmpty().WithMessage("Platform ID cannot be empty.");

        RuleFor(x => x.MoodIds)
            .Must(ids => ids == null || ids.Count <= 50)
            .WithMessage("Mood IDs cannot exceed 50 items.");

        RuleForEach(x => x.MoodIds)
            .NotEmpty().WithMessage("Mood ID cannot be empty.");

        RuleFor(x => x.GenreIds)
            .Must(ids => ids == null || ids.Count <= 50)
            .WithMessage("Genre IDs cannot exceed 50 items.");

        RuleForEach(x => x.GenreIds)
            .NotEmpty().WithMessage("Genre ID cannot be empty.");
    }
}
