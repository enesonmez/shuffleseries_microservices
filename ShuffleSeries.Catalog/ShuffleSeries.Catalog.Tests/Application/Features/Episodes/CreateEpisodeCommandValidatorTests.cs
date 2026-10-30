using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.CreateEpisode;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes;

public class CreateEpisodeCommandValidatorTests
{
    private readonly CreateEpisodeCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        var command = new CreateEpisodeCommand(
            SeriesId: Guid.NewGuid(),
            SeasonNumber: 1,
            EpisodeNumber: 1,
            Title: "Blink",
            Overview: "Don't blink",
            LanguageCode: "en",
            IsStandalone: true,
            RuntimeMinutes: 45,
            ImdbRating: 9.8,
            ThumbnailUrl: "https://example.com/blink.jpg");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_SeriesIdIsEmpty()
    {
        var command = new CreateEpisodeCommand(
            SeriesId: Guid.Empty,
            SeasonNumber: 1,
            EpisodeNumber: 1,
            Title: "Blink",
            Overview: null,
            LanguageCode: "en");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.SeriesId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Fail_When_SeasonNumberIsZeroOrNegative(int season)
    {
        var command = new CreateEpisodeCommand(
            SeriesId: Guid.NewGuid(),
            SeasonNumber: season,
            EpisodeNumber: 1,
            Title: "Blink",
            Overview: null,
            LanguageCode: "en");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.SeasonNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Fail_When_EpisodeNumberIsZeroOrNegative(int episode)
    {
        var command = new CreateEpisodeCommand(
            SeriesId: Guid.NewGuid(),
            SeasonNumber: 1,
            EpisodeNumber: episode,
            Title: "Blink",
            Overview: null,
            LanguageCode: "en");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.EpisodeNumber);
    }

    [Fact]
    public void Should_Fail_When_TitleIsEmpty()
    {
        var command = new CreateEpisodeCommand(
            SeriesId: Guid.NewGuid(),
            SeasonNumber: 1,
            EpisodeNumber: 1,
            Title: "",
            Overview: null,
            LanguageCode: "en");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-lang")]
    public void Should_Fail_When_LanguageCodeIsInvalid(string lang)
    {
        var command = new CreateEpisodeCommand(
            SeriesId: Guid.NewGuid(),
            SeasonNumber: 1,
            EpisodeNumber: 1,
            Title: "Blink",
            Overview: null,
            LanguageCode: lang);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.LanguageCode);
    }
}
