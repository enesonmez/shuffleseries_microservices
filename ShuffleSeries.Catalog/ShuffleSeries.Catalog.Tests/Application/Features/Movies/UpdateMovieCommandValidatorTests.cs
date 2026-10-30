using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Movies.Commands.UpdateMovie;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Movies;

public class UpdateMovieCommandValidatorTests
{
    private readonly UpdateMovieCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_ValidCommand()
    {
        var command = new UpdateMovieCommand(
            Id: Guid.NewGuid(),
            TmdbId: 550,
            ImdbId: "tt1234567",
            ReleaseYear: 2022,
            RuntimeMinutes: 120,
            Director: "Christopher Nolan",
            ImdbRating: 8.8,
            ImdbVotes: 10000);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        var command = new UpdateMovieCommand(
            Id: Guid.Empty,
            TmdbId: 550,
            ImdbId: "tt1234567",
            ReleaseYear: 2022,
            RuntimeMinutes: 120,
            Director: null,
            ImdbRating: null,
            ImdbVotes: null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Should_Fail_When_TmdbIdIsZeroOrNegative()
    {
        var command = new UpdateMovieCommand(
            Id: Guid.NewGuid(),
            TmdbId: 0,
            ImdbId: "tt1234567",
            ReleaseYear: 2022,
            RuntimeMinutes: 120);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TmdbId);
    }

    [Fact]
    public void Should_Fail_When_ImdbIdIsInvalid()
    {
        var command = new UpdateMovieCommand(
            Id: Guid.NewGuid(),
            TmdbId: 550,
            ImdbId: "invalid",
            ReleaseYear: 2022,
            RuntimeMinutes: 120);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ImdbId);
    }

    [Theory]
    [InlineData(1800)]
    [InlineData(2101)]
    public void Should_Fail_When_ReleaseYearIsOutOfBounds(int year)
    {
        var command = new UpdateMovieCommand(
            Id: Guid.NewGuid(),
            TmdbId: 550,
            ImdbId: "tt1234567",
            ReleaseYear: year,
            RuntimeMinutes: 120,
            Director: null,
            ImdbRating: null,
            ImdbVotes: null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ReleaseYear);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Fail_When_RuntimeMinutesIsZeroOrNegative(int runtime)
    {
        var command = new UpdateMovieCommand(
            Id: Guid.NewGuid(),
            TmdbId: 550,
            ImdbId: "tt1234567",
            ReleaseYear: 2020,
            RuntimeMinutes: runtime,
            Director: null,
            ImdbRating: null,
            ImdbVotes: null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RuntimeMinutes);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(10.1)]
    public void Should_Fail_When_ImdbRatingIsOutOfBounds(double rating)
    {
        var command = new UpdateMovieCommand(
            Id: Guid.NewGuid(),
            TmdbId: 550,
            ImdbId: "tt1234567",
            ReleaseYear: 2020,
            RuntimeMinutes: 100,
            Director: null,
            ImdbRating: rating,
            ImdbVotes: null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ImdbRating);
    }

    [Fact]
    public void Should_Fail_When_ImdbVotesIsNegative()
    {
        var command = new UpdateMovieCommand(
            Id: Guid.NewGuid(),
            TmdbId: 550,
            ImdbId: "tt1234567",
            ReleaseYear: 2020,
            RuntimeMinutes: 100,
            Director: null,
            ImdbRating: 8.0,
            ImdbVotes: -1);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ImdbVotes);
    }
}
