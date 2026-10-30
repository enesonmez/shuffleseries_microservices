using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Events;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Tests.Domain.Entities;

public class SeriesTests
{
    [Fact]
    public void Create_Should_CreateSeriesAndRaiseDomainEvent_When_ValidParameters()
    {
        // Act
        var series = Series.Create(
            tmdbId: 42009,
            imdbId: "tt2085059",
            releaseYear: 2011,
            seriesType: SeriesType.EpisodicAnthology,
            posterUrl: "https://example.com/black-mirror.jpg",
            backdropUrl: "https://example.com/bm-backdrop.jpg",
            imdbRating: 8.7,
            imdbVotes: 600000,
            totalSeasons: 6);

        // Assert
        series.Should().NotBeNull();
        series.Id.Should().NotBeEmpty();
        series.MediaType.Should().Be(MediaType.Series);
        series.TmdbId.Should().Be(42009);
        series.ImdbId.Should().Be("tt2085059");
        series.SeriesType.Should().Be(SeriesType.EpisodicAnthology);
        series.ReleaseYear.Should().Be(2011);
        series.PosterUrl.Should().Be("https://example.com/black-mirror.jpg");
        series.BackdropUrl.Should().Be("https://example.com/bm-backdrop.jpg");
        series.ImdbRating.Should().Be(8.7);
        series.ImdbVotes.Should().Be(600000);
        series.TotalSeasons.Should().Be(6);
        series.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        // Domain event assertion
        series.GetDomainEvents().Should().ContainSingle(e => e is MediaCreatedDomainEvent);
        var domainEvent = series.GetDomainEvents().OfType<MediaCreatedDomainEvent>().First();
        domainEvent.MediaId.Should().Be(series.Id);
        domainEvent.MediaType.Should().Be(MediaType.Series);
    }

    [Fact]
    public void UpdateDetails_Should_UpdatePropertiesAndRaiseDomainEvent()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2010, SeriesType.Standard, "https://example.com/poster.jpg");
        series.ClearDomainEvents();

        // Act
        series.UpdateDetails(
            tmdbId: 42009,
            imdbId: "tt9999999",
            releaseYear: 2011,
            seriesType: SeriesType.SeasonalAnthology,
            totalSeasons: 4,
            imdbRating: 9.1,
            imdbVotes: 100000,
            posterUrl: "https://example.com/new-poster.jpg",
            backdropUrl: "https://example.com/new-backdrop.jpg");

        // Assert
        series.TmdbId.Should().Be(42009);
        series.ImdbId.Should().Be("tt9999999");
        series.ReleaseYear.Should().Be(2011);
        series.SeriesType.Should().Be(SeriesType.SeasonalAnthology);
        series.TotalSeasons.Should().Be(4);
        series.ImdbRating.Should().Be(9.1);
        series.ImdbVotes.Should().Be(100000);
        series.PosterUrl.Should().Be("https://example.com/new-poster.jpg");
        series.BackdropUrl.Should().Be("https://example.com/new-backdrop.jpg");

        series.GetDomainEvents().Should().ContainSingle(e => e is MediaUpdatedDomainEvent);
    }

    [Fact]
    public void AddSeason_Should_AddSeasonToSeries_And_UpdateTotalSeasonsIfHigher()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2020, SeriesType.Standard, "https://example.com/poster.jpg", totalSeasons: 1);

        // Act
        var season = series.AddSeason(2, "Season 2");

        // Assert
        season.Should().NotBeNull();
        season.SeasonNumber.Should().Be(2);
        season.Title.Should().Be("Season 2");
        series.Seasons.Should().Contain(season);
        series.TotalSeasons.Should().Be(2);
    }

    [Fact]
    public void AddSeason_WhenSeasonAlreadyExists_ShouldReturnExistingSeason()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2020, SeriesType.Standard, "https://example.com/poster.jpg");
        var season1 = series.AddSeason(1, "Season 1");

        // Act
        var duplicateSeason = series.AddSeason(1, "Duplicate");

        // Assert
        duplicateSeason.Should().BeSameAs(season1);
        series.Seasons.Should().HaveCount(1);
    }

    [Fact]
    public void AddEpisode_InEpisodicAnthology_ShouldDefaultIsStandaloneToTrue()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");

        // Act
        var episode = series.AddEpisode(
            seasonNumber: 1,
            episodeNumber: 1,
            isStandalone: null,
            runtimeMinutes: 44,
            imdbRating: 8.0,
            thumbnailUrl: "https://example.com/ep1.jpg");

        // Assert
        episode.Should().NotBeNull();
        episode.EpisodeNumber.Should().Be(1);
        episode.IsStandalone.Should().BeTrue();
        episode.RuntimeMinutes.Should().Be(44);
        episode.ImdbRating.Should().Be(8.0);
        series.GetDomainEvents().Should().ContainSingle(e => e is EpisodeCreatedDomainEvent)
            .Which.Should().BeEquivalentTo(new EpisodeCreatedDomainEvent(series.Id, episode.SeasonId, episode.Id, 1, true));
    }

    [Fact]
    public void AddEpisode_InStandardSeries_ShouldDefaultIsStandaloneToFalse()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2008, SeriesType.Standard, "https://example.com/bb.jpg");

        // Act
        var episode = series.AddEpisode(
            seasonNumber: 1,
            episodeNumber: 1,
            isStandalone: null);

        // Assert
        episode.IsStandalone.Should().BeFalse();
    }

    [Fact]
    public void AddOrUpdateTranslation_Should_AddTranslationNormalizedAndAllowUpdate()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2020, SeriesType.Standard, "https://example.com/poster.jpg");

        // Act 1: Add TR
        var tr = series.AddOrUpdateTranslation("TR", "Başlık", "Özet", "Etiket");

        // Assert 1
        tr.LanguageCode.Should().Be(LanguageCode.Turkish);
        tr.Title.Should().Be("Başlık");
        series.Translations.Should().HaveCount(1);

        // Act 2: Update TR
        var updatedTr = series.AddOrUpdateTranslation("tr", "Yeni Başlık", "Yeni Özet", "Yeni Etiket");

        // Assert 2
        updatedTr.Title.Should().Be("Yeni Başlık");
        series.Translations.Should().HaveCount(1);
    }

    [Fact]
    public void AddPlatform_Should_AddPlatformOrThrowIfDuplicate()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2020, SeriesType.Standard, "https://example.com/poster.jpg");
        var platformId = Guid.NewGuid();

        // Act
        var mediaPlatform = series.AddPlatform(platformId, "https://netflix.com/title/123", "netflix://title/123");

        // Assert
        mediaPlatform.PlatformId.Should().Be(platformId);
        series.MediaPlatforms.Should().Contain(mediaPlatform);

        // Duplicate
        var action = () => series.AddPlatform(platformId, "https://netflix.com/other");
        action.Should().Throw<DuplicateMediaPlatformException>();
    }

    [Fact]
    public void AddMood_Should_AddMoodOrThrowIfDuplicate()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2020, SeriesType.Standard, "https://example.com/poster.jpg");
        var moodId = Guid.NewGuid();

        // Act
        var mediaMood = series.AddMood(moodId);

        // Assert
        mediaMood.MoodId.Should().Be(moodId);
        series.MediaMoods.Should().Contain(mediaMood);

        // Duplicate
        var action = () => series.AddMood(moodId);
        action.Should().Throw<DuplicateMediaMoodException>();
    }

    [Fact]
    public void Delete_Should_SoftDeleteAndRaiseDomainEvent()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2020, SeriesType.Standard, "https://example.com/poster.jpg");
        series.ClearDomainEvents();

        // Act
        series.Delete("admin");

        // Assert
        series.IsDeleted.Should().BeTrue();
        series.DeletedBy.Should().Be("admin");
        series.DeletedAtUtc.Should().NotBeNull();
        series.GetDomainEvents().Should().ContainSingle(e => e is MediaDeletedDomainEvent);
    }

    [Fact]
    public void Delete_Should_CascadeSoftDeleteSeasonsAndEpisodes()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2020, SeriesType.Standard, "https://example.com/poster.jpg");
        var season = series.AddSeason(1);
        var episode = series.AddEpisode(1, 1, isStandalone: false, runtimeMinutes: 45);

        // Act
        series.Delete("system-admin");

        // Assert
        series.IsDeleted.Should().BeTrue();
        season.IsDeleted.Should().BeTrue();
        season.DeletedBy.Should().Be("system-admin");
        season.DeletedAtUtc.Should().NotBeNull();

        episode.IsDeleted.Should().BeTrue();
        episode.DeletedBy.Should().Be("system-admin");
        episode.DeletedAtUtc.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_ThrowArgumentException_When_PosterUrlIsEmpty(string invalidPosterUrl)
    {
        var action = () => Series.Create(42009, "tt2085059", 2020, SeriesType.Standard, invalidPosterUrl);
        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(1800)]
    [InlineData(2101)]
    public void Create_Should_ThrowArgumentOutOfRangeException_When_ReleaseYearIsOutOfBounds(int invalidYear)
    {
        var action = () => Series.Create(42009, "tt2085059", invalidYear, SeriesType.Standard, "https://example.com/poster.jpg");
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddEpisode_WhenDuplicateEpisodeNumberInSameSeason_ShouldThrowDuplicateEpisodeException()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg");
        series.AddEpisode(1, 1);

        // Act
        var action = () => series.AddEpisode(1, 1);

        // Assert
        action.Should().Throw<DuplicateEpisodeException>();
    }

    [Fact]
    public void RemoveChildCollections_Should_MarkRemovedItemsForHardDelete()
    {
        // Arrange
        var series = Series.Create(42009, "tt2085059", 2020, SeriesType.Standard, "https://example.com/poster.jpg");
        var translation = series.AddOrUpdateTranslation("tr", "Başlık", "Özet");
        var platformId = Guid.NewGuid();
        var mediaPlatform = series.AddPlatform(platformId, "https://netflix.com/title/1");
        var moodId = Guid.NewGuid();
        var mediaMood = series.AddMood(moodId);
        var genreId = Guid.NewGuid();
        var mediaGenre = series.AddGenre(genreId);

        // Act
        series.RemoveTranslation("tr");
        series.RemovePlatform(platformId);
        series.RemoveMood(moodId);
        series.RemoveGenre(genreId);

        // Assert
        translation.IsHardDeleteRequested.Should().BeTrue();
        mediaPlatform.IsHardDeleteRequested.Should().BeTrue();
        mediaMood.IsHardDeleteRequested.Should().BeTrue();
        mediaGenre.IsHardDeleteRequested.Should().BeTrue();
    }
}
