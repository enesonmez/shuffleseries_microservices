using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;

namespace ShuffleSeries.Catalog.Tests.Infrastructure.Persistence.Repositories;

public class EpisodeRepositoryTests
{
    private static CatalogDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task GetStandaloneEpisodesWithSeriesContextAsync_Should_ReturnActiveStandaloneWithContext_And_ExcludeGhostEpisodes()
    {
        await using var context = CreateInMemoryContext();

        var platform = Platform.Create("Netflix", "netflix", "https://example.com/n.svg", "https://example.com/ni.svg", "https://netflix.com");
        var mood = Mood.Create("beyin-yakanlar", "#8B5CF6", "brain", 1);
        var genre = Genre.Create("science-fiction", 1);
        context.Platforms.Add(platform);
        context.Moods.Add(mood);
        context.Genres.Add(genre);

        var activeSeries = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg", imdbRating: 8.7);
        activeSeries.AddOrUpdateTranslation("en", "Black Mirror", "Anthology");
        activeSeries.AddPlatform(platform.Id, "https://netflix.com/title/70264888");
        activeSeries.AddMood(mood.Id);
        activeSeries.AddGenre(genre.Id);

        var standaloneEp = activeSeries.AddEpisode(1, 1, isStandalone: true, runtimeMinutes: 44, imdbRating: 8.2);
        standaloneEp.AddOrUpdateTranslation("en", "The National Anthem", "Episode overview");
        var nonStandaloneEp = activeSeries.AddEpisode(1, 2, isStandalone: false, runtimeMinutes: 50, imdbRating: 7.5);
        nonStandaloneEp.AddOrUpdateTranslation("en", "Serialized", "Not standalone");

        var deletedSeries = Series.Create(99999, "tt9999999", 2019, SeriesType.EpisodicAnthology, "https://example.com/del.jpg");
        deletedSeries.AddOrUpdateTranslation("en", "Deleted Series", "Deleted");
        var ghostEpisode = deletedSeries.AddEpisode(1, 1, isStandalone: true, runtimeMinutes: 40, imdbRating: 9.9);
        ghostEpisode.AddOrUpdateTranslation("en", "Ghost Episode", "Should not leak");
        deletedSeries.SoftDelete("admin");

        context.Series.AddRange(activeSeries, deletedSeries);
        await context.SaveChangesAsync();

        var repository = new EpisodeRepository(context);

        var (results, totalCount) = await repository.GetStandaloneEpisodesWithSeriesContextAsync(1, 50, CancellationToken.None);

        totalCount.Should().Be(1);
        results.Should().ContainSingle();
        results[0].Episode.Id.Should().Be(standaloneEp.Id);
        results[0].Season.SeasonNumber.Should().Be(1);
        results[0].Series.Id.Should().Be(activeSeries.Id);
        results[0].Series.MediaMoods.Should().ContainSingle();
        results[0].Series.MediaGenres.Should().ContainSingle();
    }

    [Fact]
    public async Task GetStandaloneEpisodesWithSeriesContextAsync_Should_PaginateCleanly_And_ReturnEmptyOnOutOfRangePage()
    {
        await using var context = CreateInMemoryContext();

        var activeSeries = Series.Create(42010, "tt2085060", 2015, SeriesType.EpisodicAnthology, "https://example.com/bm2.jpg");
        var ep1 = activeSeries.AddEpisode(1, 1, isStandalone: true, runtimeMinutes: 30, imdbRating: 9.0);
        var ep2 = activeSeries.AddEpisode(1, 2, isStandalone: true, runtimeMinutes: 35, imdbRating: 8.5);
        var ep3 = activeSeries.AddEpisode(1, 3, isStandalone: true, runtimeMinutes: 40, imdbRating: 8.0);
        ep1.AddOrUpdateTranslation("en", "Ep 1");
        ep2.AddOrUpdateTranslation("en", "Ep 2");
        ep3.AddOrUpdateTranslation("en", "Ep 3");

        context.Series.Add(activeSeries);
        await context.SaveChangesAsync();

        var repository = new EpisodeRepository(context);

        var (page1, total1) = await repository.GetStandaloneEpisodesWithSeriesContextAsync(1, 2, CancellationToken.None);
        var (page2, total2) = await repository.GetStandaloneEpisodesWithSeriesContextAsync(2, 2, CancellationToken.None);
        var (page3, total3) = await repository.GetStandaloneEpisodesWithSeriesContextAsync(3, 2, CancellationToken.None);

        total1.Should().Be(3);
        total2.Should().Be(3);
        total3.Should().Be(3);
        page1.Select(x => x.Episode.Id).Should().Equal(ep1.Id, ep2.Id);
        page2.Select(x => x.Episode.Id).Should().Equal(ep3.Id);
        page3.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStandaloneEpisodesWithSeriesContextAsync_Should_ReturnEmpty_When_NoStandaloneEpisodesExist()
    {
        await using var context = CreateInMemoryContext();
        var repository = new EpisodeRepository(context);

        var (results, totalCount) = await repository.GetStandaloneEpisodesWithSeriesContextAsync(1, 10, CancellationToken.None);
        var plainStandalone = await repository.GetStandaloneEpisodesAsync(null, CancellationToken.None);

        totalCount.Should().Be(0);
        results.Should().BeEmpty();
        plainStandalone.Should().BeEmpty();
    }
}
