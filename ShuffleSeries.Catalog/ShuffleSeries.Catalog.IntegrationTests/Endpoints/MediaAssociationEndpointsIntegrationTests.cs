using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Api.Contracts.Requests.MediaAssociations;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Movies;
using ShuffleSeries.Catalog.IntegrationTests.Infrastructure;

namespace ShuffleSeries.Catalog.IntegrationTests.Endpoints;

public class MediaAssociationEndpointsIntegrationTests : CatalogIntegrationTestBase
{
    public MediaAssociationEndpointsIntegrationTests(CatalogApiFactory factory) : base(factory)
    {
    }

    private async Task<Guid> CreateSampleMovieAsync()
    {
        var request = new CreateMovieRequest(
            TmdbId: 278,
            ImdbId: "tt0111161",
            ReleaseYear: 1994,
            RuntimeMinutes: 142,
            PosterUrl: "https://example.com/shawshank.jpg",
            BackdropUrl: null,
            LanguageCode: "tr",
            Title: "Esaretin Bedeli",
            Overview: "Umut iyi bir şeydir.");

        var response = await Client.PostAsJsonAsync("api/catalog/movies", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task AddAndRemoveMediaPlatform_ShouldModifyAssociationsInPostgreSql()
    {
        // Arrange
        var movieId = await CreateSampleMovieAsync();
        Guid platformId = Guid.Empty;
        await ExecuteDbContextAsync(async context =>
        {
            platformId = (await context.Platforms.FirstAsync(p => p.Slug == "netflix")).Id;
        });

        var addRequest = new AddMediaPlatformRequest(platformId, "https://netflix.com/watch/123", "netflix://watch/123");

        // Act 1: Add platform
        var addResponse = await Client.PostAsJsonAsync($"api/catalog/media/{movieId}/platforms", addRequest);
        addResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB
        await ExecuteDbContextAsync(async context =>
        {
            var mp = await context.MediaPlatforms.FirstOrDefaultAsync(x => x.MediaId == movieId && x.PlatformId == platformId);
            mp.Should().NotBeNull();
            mp!.WebUrl.Should().Be("https://netflix.com/watch/123");
        });

        // Act 2: Remove platform
        var removeResponse = await Client.DeleteAsync($"api/catalog/media/{movieId}/platforms/{platformId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB
        await ExecuteDbContextAsync(async context =>
        {
            var mp = await context.MediaPlatforms.FirstOrDefaultAsync(x => x.MediaId == movieId && x.PlatformId == platformId);
            mp.Should().BeNull();
        });
    }

    [Fact]
    public async Task AddAndRemoveMediaMood_ShouldModifyAssociationsInPostgreSql()
    {
        // Arrange
        var movieId = await CreateSampleMovieAsync();
        Guid moodId = Guid.Empty;
        await ExecuteDbContextAsync(async context =>
        {
            moodId = (await context.Moods.FirstAsync(m => m.Code == "icini-isitanlar")).Id;
        });

        var addRequest = new AddMediaMoodRequest(moodId);

        // Act 1: Add mood
        var addResponse = await Client.PostAsJsonAsync($"api/catalog/media/{movieId}/moods", addRequest);
        addResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB
        await ExecuteDbContextAsync(async context =>
        {
            var mm = await context.MediaMoods.FirstOrDefaultAsync(x => x.MediaId == movieId && x.MoodId == moodId);
            mm.Should().NotBeNull();
        });

        // Act 2: Remove mood
        var removeResponse = await Client.DeleteAsync($"api/catalog/media/{movieId}/moods/{moodId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB
        await ExecuteDbContextAsync(async context =>
        {
            var mm = await context.MediaMoods.FirstOrDefaultAsync(x => x.MediaId == movieId && x.MoodId == moodId);
            mm.Should().BeNull();
        });
    }

    [Fact]
    public async Task AddAndRemoveMediaGenre_ShouldModifyAssociationsInPostgreSql()
    {
        // Arrange
        var movieId = await CreateSampleMovieAsync();
        Guid genreId = Guid.Empty;
        await ExecuteDbContextAsync(async context =>
        {
            genreId = (await context.Genres.FirstAsync(g => g.Code == "drama")).Id;
        });

        var addRequest = new AddMediaGenreRequest(genreId);

        // Act 1: Add genre
        var addResponse = await Client.PostAsJsonAsync($"api/catalog/media/{movieId}/genres", addRequest);
        addResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB
        await ExecuteDbContextAsync(async context =>
        {
            var mg = await context.MediaGenres.FirstOrDefaultAsync(x => x.MediaId == movieId && x.GenreId == genreId);
            mg.Should().NotBeNull();
        });

        // Act 2: Remove genre
        var removeResponse = await Client.DeleteAsync($"api/catalog/media/{movieId}/genres/{genreId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB
        await ExecuteDbContextAsync(async context =>
        {
            var mg = await context.MediaGenres.FirstOrDefaultAsync(x => x.MediaId == movieId && x.GenreId == genreId);
            mg.Should().BeNull();
        });
    }

    [Fact]
    public async Task AddOrUpdateTranslation_AndRemoveTranslation_ShouldModifyInPostgreSql()
    {
        // Arrange
        var movieId = await CreateSampleMovieAsync();
        var translationRequest = new AddOrUpdateTranslationRequest(
            LanguageCode: "en",
            Title: "The Shawshank Redemption",
            Overview: "Hope is a good thing.",
            Tagline: "Fear can hold you prisoner. Hope can set you free.");

        // Act 1: Add English translation
        var postResponse = await Client.PostAsJsonAsync($"api/catalog/media/{movieId}/translations", translationRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB
        await ExecuteDbContextAsync(async context =>
        {
            var tr = await context.MediaTranslations.FirstOrDefaultAsync(x => x.MediaId == movieId && x.LanguageCode == "en");
            tr.Should().NotBeNull();
            tr!.Title.Should().Be("The Shawshank Redemption");
        });

        // Act 2: Remove English translation
        var deleteResponse = await Client.DeleteAsync($"api/catalog/media/{movieId}/translations/en");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB
        await ExecuteDbContextAsync(async context =>
        {
            var tr = await context.MediaTranslations.FirstOrDefaultAsync(x => x.MediaId == movieId && x.LanguageCode == "en");
            tr.Should().BeNull();
        });
    }
}
