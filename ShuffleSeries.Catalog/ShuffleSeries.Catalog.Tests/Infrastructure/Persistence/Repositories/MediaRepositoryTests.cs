using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;

namespace ShuffleSeries.Catalog.Tests.Infrastructure.Persistence.Repositories;

public class MediaRepositoryTests
{
    private static CatalogDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task GetBulkForShuffleAsync_Should_FilterByMediaType_And_ExcludeSoftDeleted()
    {
        await using var context = CreateInMemoryContext();

        var platform = Platform.Create("Netflix", "netflix", "https://example.com/n.svg", "https://example.com/ni.svg", "https://netflix.com");
        var mood = Mood.Create("beyin-yakanlar", "#8B5CF6", "brain", 1);
        var genre = Genre.Create("science-fiction", 1);
        context.Platforms.Add(platform);
        context.Moods.Add(mood);
        context.Genres.Add(genre);

        var activeMovie = Movie.Create(1, "tt0000001", 2020, 120, "https://example.com/m1.jpg", imdbRating: 8.5);
        activeMovie.AddOrUpdateTranslation("en", "Active Movie", "Overview");
        activeMovie.AddPlatform(platform.Id, "https://netflix.com/1");
        activeMovie.AddMood(mood.Id);
        activeMovie.AddGenre(genre.Id);

        var deletedMovie = Movie.Create(2, "tt0000002", 2021, 110, "https://example.com/m2.jpg", imdbRating: 9.0);
        deletedMovie.Delete("admin");

        var activeSeries = Series.Create(3, "tt0000003", 2022, SeriesType.EpisodicAnthology, "https://example.com/s1.jpg", imdbRating: 8.9);
        activeSeries.AddOrUpdateTranslation("tr", "Aktif Dizi", "Özet");

        context.Movies.AddRange(activeMovie, deletedMovie);
        context.Series.Add(activeSeries);
        await context.SaveChangesAsync();

        var repository = new MediaRepository(context);

        var (allItems, allCount) = await repository.GetBulkForShuffleAsync(null, 1, 10, CancellationToken.None);
        allCount.Should().Be(2);
        allItems.Should().HaveCount(2);

        var (movieItems, movieCount) = await repository.GetBulkForShuffleAsync(MediaType.Movie, 1, 10, CancellationToken.None);
        movieCount.Should().Be(1);
        movieItems.Should().ContainSingle(m => m.Id == activeMovie.Id);
        movieItems[0].MediaPlatforms.Should().ContainSingle(mp => mp.Platform != null && mp.Platform.Slug == "netflix");

        var fetched = await repository.GetByIdForShuffleAsync(activeMovie.Id, CancellationToken.None);
        fetched.Should().NotBeNull();
        fetched!.Translations.Should().ContainSingle();
        fetched.MediaMoods.Should().ContainSingle();
        fetched.MediaGenres.Should().ContainSingle();

        var deletedFetch = await repository.GetByIdForShuffleAsync(deletedMovie.Id, CancellationToken.None);
        deletedFetch.Should().BeNull();
    }

    [Fact]
    public async Task GetBulkForShuffleAsync_Should_ExcludeSoftDeletedTaxonomies_From_LoadedAssociations()
    {
        await using var context = CreateInMemoryContext();

        var activePlatform = Platform.Create("Netflix", "netflix", "https://example.com/n.svg", "https://example.com/ni.svg", "https://netflix.com");
        var deletedPlatform = Platform.Create("OldPlatform", "old-platform", "https://example.com/o.svg", "https://example.com/oi.svg", "https://old.com");
        deletedPlatform.SoftDelete("admin");

        var activeMood = Mood.Create("beyin-yakanlar", "#8B5CF6", "brain", 1);
        var deletedMood = Mood.Create("eski-mod", "#111111", "moon", 2);
        deletedMood.SoftDelete("admin");

        var activeGenre = Genre.Create("science-fiction", 1);
        var deletedGenre = Genre.Create("silinmis-tur", 2);
        deletedGenre.SoftDelete("admin");

        context.Platforms.AddRange(activePlatform, deletedPlatform);
        context.Moods.AddRange(activeMood, deletedMood);
        context.Genres.AddRange(activeGenre, deletedGenre);

        var movie = Movie.Create(10, "tt0000010", 2023, 125, "https://example.com/m10.jpg");
        movie.AddOrUpdateTranslation("en", "Clean Movie", "Overview");
        movie.AddPlatform(activePlatform.Id, "https://netflix.com/10");
        movie.AddPlatform(deletedPlatform.Id, "https://old.com/10");
        movie.AddMood(activeMood.Id);
        movie.AddMood(deletedMood.Id);
        movie.AddGenre(activeGenre.Id);
        movie.AddGenre(deletedGenre.Id);

        context.Movies.Add(movie);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new MediaRepository(context);
        var fetched = await repository.GetByIdForShuffleAsync(movie.Id, CancellationToken.None);

        fetched.Should().NotBeNull();
        fetched!.MediaPlatforms.Should().ContainSingle(mp => mp.PlatformId == activePlatform.Id);
        fetched.MediaMoods.Should().ContainSingle(mm => mm.MoodId == activeMood.Id);
        fetched.MediaGenres.Should().ContainSingle(mg => mg.GenreId == activeGenre.Id);
    }

    [Fact]
    public async Task GetBulkForShuffleAsync_Should_PaginateDeterministically_OrderedById()
    {
        await using var context = CreateInMemoryContext();

        var movie1 = Movie.Create(11, "tt0000011", 2020, 100, "https://example.com/11.jpg");
        var movie2 = Movie.Create(12, "tt0000012", 2021, 105, "https://example.com/12.jpg");
        movie1.AddOrUpdateTranslation("en", "Movie One", "Overview 1");
        movie2.AddOrUpdateTranslation("en", "Movie Two", "Overview 2");

        context.Movies.AddRange(movie1, movie2);
        await context.SaveChangesAsync();

        var expectedOrder = new[] { movie1.Id, movie2.Id }.OrderBy(id => id).ToArray();
        var repository = new MediaRepository(context);

        var (page1, total1) = await repository.GetBulkForShuffleAsync(null, 1, 1, CancellationToken.None);
        var (page2, total2) = await repository.GetBulkForShuffleAsync(null, 2, 1, CancellationToken.None);
        var (page3, total3) = await repository.GetBulkForShuffleAsync(null, 3, 1, CancellationToken.None);

        total1.Should().Be(2);
        total2.Should().Be(2);
        total3.Should().Be(2);
        page1.Select(m => m.Id).Should().Equal(expectedOrder[0]);
        page2.Select(m => m.Id).Should().Equal(expectedOrder[1]);
        page3.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBulkForShuffleAsync_Should_ReturnEmpty_When_NoMediaMatchesFilter()
    {
        await using var context = CreateInMemoryContext();
        var repository = new MediaRepository(context);

        var (items, totalCount) = await repository.GetBulkForShuffleAsync(MediaType.Movie, 1, 10, CancellationToken.None);

        totalCount.Should().Be(0);
        items.Should().BeEmpty();
    }
}
