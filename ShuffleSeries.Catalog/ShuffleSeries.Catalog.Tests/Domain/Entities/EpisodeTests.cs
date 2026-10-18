using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Tests.Domain.Entities;

public class EpisodeTests
{
    [Fact]
    public void CreateEpisode_ViaSeriesAndSeason_ShouldInstantiateProperly()
    {
        // Arrange
        var series = Series.Create(2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
        var season = series.AddSeason(1, "Season 1");

        // Act
        var episode = season.AddEpisode(
            episodeNumber: 1,
            isStandalone: true,
            runtimeMinutes: 44,
            imdbRating: 8.5,
            thumbnailUrl: "https://example.com/ep1.jpg");

        // Assert
        episode.Should().NotBeNull();
        episode.SeasonId.Should().Be(season.Id);
        episode.EpisodeNumber.Should().Be(1);
        episode.IsStandalone.Should().BeTrue();
        episode.RuntimeMinutes.Should().Be(44);
        episode.ImdbRating.Should().Be(8.5);
        episode.ThumbnailUrl.Should().Be("https://example.com/ep1.jpg");
    }

    [Fact]
    public void AddOrUpdateTranslation_Should_AddAndNormalizeLanguageCode()
    {
        // Arrange
        var series = Series.Create(2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
        var season = series.AddSeason(1);
        var episode = season.AddEpisode(1, true);

        // Act
        var translation = episode.AddOrUpdateTranslation("EN", "The National Anthem", "A prime minister faces a dilemma.");

        // Assert
        translation.LanguageCode.Should().Be(LanguageCode.English);
        translation.Title.Should().Be("The National Anthem");
        translation.Overview.Should().Be("A prime minister faces a dilemma.");
        episode.Translations.Should().Contain(translation);
    }

    [Fact]
    public void AddPlatform_Should_AddOrUpdateUrlsIfPlatformAlreadyExists()
    {
        // Arrange
        var series = Series.Create(2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
        var season = series.AddSeason(1);
        var episode = season.AddEpisode(1, true);
        var platformId = Guid.NewGuid();

        // Act 1: Add
        var epPlatform = episode.AddPlatform(platformId, "https://netflix.com/watch/123", "netflix://watch/123");

        // Assert 1
        epPlatform.PlatformId.Should().Be(platformId);
        episode.EpisodePlatforms.Should().Contain(epPlatform);

        // Act 2: Add same platform with updated URLs
        var updatedPlatform = episode.AddPlatform(platformId, "https://netflix.com/watch/456", "netflix://watch/456");

        // Assert 2: Should update existing without duplicate
        updatedPlatform.Should().BeSameAs(epPlatform);
        updatedPlatform.WebUrl.Should().Be("https://netflix.com/watch/456");
        episode.EpisodePlatforms.Should().HaveCount(1);
    }

    [Fact]
    public void SetStandalone_Should_UpdateFlagAndModifiedTime()
    {
        // Arrange
        var series = Series.Create(2020, SeriesType.Standard, "https://example.com/poster.jpg");
        var episode = series.AddEpisode(1, 1, isStandalone: false);

        // Act
        episode.SetStandalone(true);

        // Assert
        episode.IsStandalone.Should().BeTrue();
        episode.ModifiedAtUtc.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public void AddEpisode_Should_ThrowArgumentOutOfRangeException_When_RuntimeMinutesIsInvalid(int invalidRuntime)
    {
        // Arrange
        var series = Series.Create(2020, SeriesType.Standard, "https://example.com/poster.jpg");

        // Act
        var action = () => series.AddEpisode(1, 1, runtimeMinutes: invalidRuntime);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RemoveTranslationAndPlatform_Should_MarkRemovedItemsForHardDelete()
    {
        // Arrange
        var series = Series.Create(2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
        var episode = series.AddEpisode(1, 1);
        var translation = episode.AddOrUpdateTranslation("en", "Title", "Overview");
        var platformId = Guid.NewGuid();
        var epPlatform = episode.AddPlatform(platformId, "https://netflix.com/watch/1");

        // Act
        episode.RemoveTranslation("en");
        episode.RemovePlatform(platformId);

        // Assert
        translation.IsHardDeleteRequested.Should().BeTrue();
        epPlatform.IsHardDeleteRequested.Should().BeTrue();
    }
}
