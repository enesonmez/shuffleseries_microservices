using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Events;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Tests.Domain.Entities;

public class EpisodeTests
{
    [Fact]
    public void CreateEpisode_ViaSeriesAndSeason_ShouldInstantiateProperly()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
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
        var series = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
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
    public void AddPlatform_Should_AddPlatformOrThrowIfDuplicate()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
        var season = series.AddSeason(1);
        var episode = season.AddEpisode(1, true);
        var platformId = Guid.NewGuid();

        // Act 1: Add
        var epPlatform = episode.AddPlatform(platformId, "https://netflix.com/watch/123", "netflix://watch/123");

        // Assert 1
        epPlatform.PlatformId.Should().Be(platformId);
        episode.EpisodePlatforms.Should().Contain(epPlatform);

        // Act 2: Add same platform should throw DuplicateEpisodePlatformException
        var action = () => episode.AddPlatform(platformId, "https://netflix.com/watch/456", "netflix://watch/456");

        // Assert 2
        action.Should().Throw<DuplicateEpisodePlatformException>()
            .WithMessage($"Episode '{episode.Id}' is already associated with platform '{platformId}'.");
        episode.EpisodePlatforms.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public void AddEpisode_Should_ThrowArgumentOutOfRangeException_When_RuntimeMinutesIsInvalid(int invalidRuntime)
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2020, SeriesType.Standard, "https://example.com/poster.jpg");

        // Act
        var action = () => series.AddEpisode(1, 1, runtimeMinutes: invalidRuntime);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RemoveTranslationAndPlatform_Should_MarkRemovedItemsForHardDelete()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
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
        episode.Translations.Should().BeEmpty();
        episode.EpisodePlatforms.Should().BeEmpty();
    }

    [Fact]
    public void UpdateDetails_Should_UpdateAllFields_When_ParametersAreValid()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
        var episode = series.AddEpisode(1, 1, isStandalone: false, runtimeMinutes: 40, imdbRating: 7.5, thumbnailUrl: "https://example.com/ep1.jpg");

        // Act
        episode.UpdateDetails(
            episodeNumber: 2,
            isStandalone: true,
            runtimeMinutes: 55,
            imdbRating: 8.9,
            thumbnailUrl: "  https://example.com/ep2.jpg  ");

        // Assert
        episode.EpisodeNumber.Should().Be(2);
        episode.IsStandalone.Should().BeTrue();
        episode.RuntimeMinutes.Should().Be(55);
        episode.ImdbRating.Should().Be(8.9);
        episode.ThumbnailUrl.Should().Be("https://example.com/ep2.jpg");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateDetails_Should_ThrowArgumentOutOfRangeException_When_EpisodeNumberIsInvalid(int invalidEpisodeNumber)
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
        var episode = series.AddEpisode(1, 1);

        // Act
        var action = () => episode.UpdateDetails(invalidEpisodeNumber, true);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Episode number must be greater than zero.*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void UpdateDetails_Should_ThrowArgumentOutOfRangeException_When_RuntimeMinutesIsInvalid(int invalidRuntime)
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
        var episode = series.AddEpisode(1, 1);

        // Act
        var action = () => episode.UpdateDetails(1, true, runtimeMinutes: invalidRuntime);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Runtime minutes must be greater than zero.*");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(10.1)]
    public void UpdateDetails_Should_ThrowArgumentOutOfRangeException_When_ImdbRatingIsInvalid(double invalidRating)
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
        var episode = series.AddEpisode(1, 1);

        // Act
        var action = () => episode.UpdateDetails(1, true, imdbRating: invalidRating);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*IMDb rating must be between 0.0 and 10.0.*");
    }

    [Fact]
    public void Episode_Operations_Should_RaiseAppropriateDomainEvents_WithDeduplication()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
        var episode = series.AddEpisode(1, 1);
        episode.ClearDomainEvents();

        var platformId = Guid.NewGuid();

        // 1. UpdateDetails -> EpisodeUpdatedDomainEvent
        episode.UpdateDetails(2, true, 55, 8.5, "https://thumb.jpg");
        episode.GetDomainEvents().Should().ContainSingle(e => e is EpisodeUpdatedDomainEvent)
            .Which.Should().BeEquivalentTo(new EpisodeUpdatedDomainEvent(episode.Id, 2, true, 55, 8.5));

        // 2. Deduplication check: Subsequent mutations in same scope should not duplicate EpisodeUpdatedDomainEvent
        episode.AddPlatform(platformId, "https://netflix.com/watch/1");
        episode.AddOrUpdateTranslation("en", "White Christmas", "Special episode");
        episode.GetDomainEvents().Should().ContainSingle(e => e is EpisodeUpdatedDomainEvent);
        episode.ClearDomainEvents();

        // 3. RemovePlatform -> EpisodeUpdatedDomainEvent
        episode.RemovePlatform(platformId);
        episode.GetDomainEvents().Should().ContainSingle(e => e is EpisodeUpdatedDomainEvent);
        episode.ClearDomainEvents();

        // 4. RemoveTranslation -> EpisodeUpdatedDomainEvent
        episode.RemoveTranslation("en");
        episode.GetDomainEvents().Should().ContainSingle(e => e is EpisodeUpdatedDomainEvent);
        episode.ClearDomainEvents();

        // 5. Delete -> EpisodeDeletedDomainEvent
        episode.Delete();
        episode.GetDomainEvents().Should().ContainSingle(e => e is EpisodeDeletedDomainEvent)
            .Which.Should().BeEquivalentTo(new EpisodeDeletedDomainEvent(episode.Id));
    }
}
