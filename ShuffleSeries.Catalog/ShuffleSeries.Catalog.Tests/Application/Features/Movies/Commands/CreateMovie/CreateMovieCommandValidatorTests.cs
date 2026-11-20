using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Movies.Commands.CreateMovie;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Movies.Commands.CreateMovie;

public class CreateMovieCommandValidatorTests
{
    private readonly CreateMovieCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_When_TitleIsEmpty()
    {
        var command = new CreateMovieCommand(
            550,
            "tt0137523",
            2020,
            120,
            "https://example.com/poster.jpg",
            null,
            "tr",
            "",
            "Overview");

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_HaveError_When_TmdbIdIsZeroOrNegative()
    {
        var command = new CreateMovieCommand(
            0,
            "tt0137523",
            2020,
            120,
            "https://example.com/poster.jpg",
            null,
            "tr",
            "Valid Title",
            "Overview");

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TmdbId);
    }

    [Fact]
    public void Should_HaveError_When_ImdbIdIsInvalid()
    {
        var command = new CreateMovieCommand(
            550,
            "invalid-imdb",
            2020,
            120,
            "https://example.com/poster.jpg",
            null,
            "tr",
            "Valid Title",
            "Overview");

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ImdbId);
    }

    [Fact]
    public void Should_HaveError_When_ReleaseYearIsInvalid()
    {
        var command = new CreateMovieCommand(
            550,
            "tt0137523",
            1700,
            120,
            "https://example.com/poster.jpg",
            null,
            "tr",
            "Valid Title",
            "Overview");

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ReleaseYear);
    }

    [Fact]
    public void Should_NotHaveError_When_CommandIsValid()
    {
        var command = new CreateMovieCommand(
            550,
            "tt0137523",
            2023,
            120,
            "https://example.com/poster.jpg",
            "https://example.com/backdrop.jpg",
            "tr",
            "Valid Title",
            "Overview");

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/image.png")]
    [InlineData("javascript:alert(1)")]
    public void Should_HaveError_When_PosterUrl_IsInvalid(string invalidUrl)
    {
        var command = new CreateMovieCommand(
            550,
            "tt0137523",
            2023,
            120,
            invalidUrl,
            null,
            "tr",
            "Valid Title",
            "Overview");

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PosterUrl);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/image.png")]
    [InlineData("javascript:alert(1)")]
    public void Should_HaveError_When_BackdropUrl_IsInvalid(string invalidUrl)
    {
        var command = new CreateMovieCommand(
            550,
            "tt0137523",
            2023,
            120,
            "https://example.com/poster.jpg",
            invalidUrl,
            "tr",
            "Valid Title",
            "Overview");

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BackdropUrl);
    }

    [Fact]
    public void Should_HaveError_When_Overview_Exceeds2000Characters()
    {
        var command = new CreateMovieCommand(
            550,
            "tt0137523",
            2023,
            120,
            "https://example.com/poster.jpg",
            null,
            "tr",
            "Valid Title",
            new string('a', 2001));

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Overview);
    }

    [Fact]
    public void Should_HaveError_When_PosterUrl_Exceeds500Characters()
    {
        var command = new CreateMovieCommand(
            550,
            "tt0137523",
            2023,
            120,
            "https://example.com/" + new string('a', 501),
            null,
            "tr",
            "Valid Title",
            "Overview");

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PosterUrl);
    }

    [Fact]
    public void Should_HaveError_When_Tagline_Exceeds500Characters()
    {
        var command = new CreateMovieCommand(
            550,
            "tt0137523",
            2023,
            120,
            "https://example.com/poster.jpg",
            null,
            "tr",
            "Valid Title",
            "Overview",
            Tagline: new string('a', 501));

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Tagline);
    }

    [Fact]
    public void Should_HaveError_When_Director_Exceeds150Characters()
    {
        var command = new CreateMovieCommand(
            550,
            "tt0137523",
            2023,
            120,
            "https://example.com/poster.jpg",
            null,
            "tr",
            "Valid Title",
            "Overview",
            Director: new string('a', 151));

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Director);
    }

    [Fact]
    public void Should_HaveError_When_PlatformIds_ContainsEmptyGuid()
    {
        var command = new CreateMovieCommand(
            550,
            "tt0137523",
            2023,
            120,
            "https://example.com/poster.jpg",
            null,
            "tr",
            "Valid Title",
            "Overview",
            PlatformIds: [Guid.Empty]);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("PlatformIds[0]");
    }
}
