using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Movies.Commands.CreateMovie;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Movies;

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
}
