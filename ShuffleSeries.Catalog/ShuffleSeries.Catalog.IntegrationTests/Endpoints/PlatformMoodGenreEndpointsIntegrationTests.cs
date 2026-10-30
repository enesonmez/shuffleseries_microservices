using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Genres;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Moods;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Platforms;
using ShuffleSeries.Catalog.Application.Features.Genres.Responses;
using ShuffleSeries.Catalog.Application.Features.Moods.Responses;
using ShuffleSeries.Catalog.Application.Features.Platforms.Responses;
using ShuffleSeries.Catalog.IntegrationTests.Infrastructure;

namespace ShuffleSeries.Catalog.IntegrationTests.Endpoints;

public class PlatformMoodGenreEndpointsIntegrationTests : CatalogIntegrationTestBase
{
    public PlatformMoodGenreEndpointsIntegrationTests(CatalogApiFactory factory) : base(factory)
    {
    }

    // --- PLATFORMS ---

    [Fact]
    public async Task GetActivePlatforms_ShouldReturnSeededPlatforms_AndServeFromCache()
    {
        // Act 1: Initial call (populates cache)
        var response1 = await Client.GetAsync("api/catalog/platforms");
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var platforms1 = await response1.Content.ReadFromJsonAsync<IReadOnlyList<PlatformResponse>>();
        platforms1.Should().NotBeNull();
        platforms1!.Count.Should().BeGreaterThanOrEqualTo(5);

        // Act 2: Second call (served from memory cache)
        var response2 = await Client.GetAsync("api/catalog/platforms");
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var platforms2 = await response2.Content.ReadFromJsonAsync<IReadOnlyList<PlatformResponse>>();
        platforms2!.Count.Should().Be(platforms1.Count);
    }

    [Fact]
    public async Task CreatePlatform_ShouldPersist_AndInvalidateCache()
    {
        // Arrange
        var request = new CreatePlatformRequest(
            Name: "Apple TV+",
            Slug: "apple-tv-plus",
            LogoUrl: "https://example.com/appletv.svg",
            InactiveLogoUrl: "https://example.com/appletv-dark.svg",
            BaseWebUrl: "https://tv.apple.com",
            DisplayOrder: 10,
            IsActive: true);

        // Act - Create platform
        var postResponse = await Client.PostAsJsonAsync("api/catalog/platforms", request);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var platformId = await postResponse.Content.ReadFromJsonAsync<Guid>();
        platformId.Should().NotBeEmpty();

        // Assert - Subsequent GET retrieves newly created platform (cache was evicted)
        var getResponse = await Client.GetAsync("api/catalog/platforms");
        var platforms = await getResponse.Content.ReadFromJsonAsync<IReadOnlyList<PlatformResponse>>();
        platforms.Should().Contain(p => p.Slug == "apple-tv-plus" && p.Name == "Apple TV+");
    }

    [Fact]
    public async Task UpdatePlatform_ShouldPersist_AndInvalidateCache()
    {
        // Arrange - Create a platform first
        var createRequest = new CreatePlatformRequest(
            Name: "Mubi Original",
            Slug: "mubi-orig",
            LogoUrl: "https://example.com/mubi.svg",
            InactiveLogoUrl: "https://example.com/mubi-dark.svg",
            BaseWebUrl: "https://mubi.com",
            DisplayOrder: 20,
            IsActive: true);

        var postResponse = await Client.PostAsJsonAsync("api/catalog/platforms", createRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var platformId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Update platform details
        var updateRequest = new UpdatePlatformRequest(
            Name: "MUBI Global",
            Slug: "mubi-global",
            LogoUrl: "https://example.com/mubi-v2.svg",
            InactiveLogoUrl: "https://example.com/mubi-dark-v2.svg",
            BaseWebUrl: "https://mubi.com/global",
            BaseAppSchemeUrl: "mubi://",
            DisplayOrder: 25);

        var putResponse = await Client.PutAsJsonAsync($"api/catalog/platforms/{platformId}", updateRequest);
        putResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert - Verify updated values in GET
        var getResponse = await Client.GetAsync("api/catalog/platforms");
        var platforms = await getResponse.Content.ReadFromJsonAsync<IReadOnlyList<PlatformResponse>>();
        var updated = platforms!.FirstOrDefault(p => p.Id == platformId);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("MUBI Global");
        updated.Slug.Should().Be("mubi-global");
        updated.LogoUrl.Should().Be("https://example.com/mubi-v2.svg");
        updated.BaseAppSchemeUrl.Should().Be("mubi://");
        updated.DisplayOrder.Should().Be(25);
    }

    [Fact]
    public async Task UpdatePlatform_WhenSlugConflict_ShouldReturn409()
    {
        // Arrange - Create first platform
        var p1 = new CreatePlatformRequest("Conflict Test 1", "conflict-slug-1", "https://example.com/1.svg", "https://example.com/1-d.svg", "https://example.com/1");
        var postRes1 = await Client.PostAsJsonAsync("api/catalog/platforms", p1);
        postRes1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Create second platform
        var p2 = new CreatePlatformRequest("Conflict Test 2", "conflict-slug-2", "https://example.com/2.svg", "https://example.com/2-d.svg", "https://example.com/2");
        var postRes2 = await Client.PostAsJsonAsync("api/catalog/platforms", p2);
        postRes2.StatusCode.Should().Be(HttpStatusCode.Created);
        var p2Id = await postRes2.Content.ReadFromJsonAsync<Guid>();

        // Act - Try updating p2 to use p1's slug
        var updateRequest = new UpdatePlatformRequest("Conflict Test 2", "conflict-slug-1", "https://example.com/2.svg", "https://example.com/2-d.svg", "https://example.com/2");
        var putResponse = await Client.PutAsJsonAsync($"api/catalog/platforms/{p2Id}", updateRequest);

        // Assert
        putResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeletePlatform_ShouldSoftDelete_AndEvictFromActiveList()
    {
        // Arrange - Create platform to delete
        var createRequest = new CreatePlatformRequest(
            Name: "BluTV Temp",
            Slug: "blutv-temp",
            LogoUrl: "https://example.com/blu.svg",
            InactiveLogoUrl: "https://example.com/blu-dark.svg",
            BaseWebUrl: "https://blutv.com");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/platforms", createRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var platformId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Delete platform
        var deleteResponse = await Client.DeleteAsync($"api/catalog/platforms/{platformId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert 1 - Subsequent GET does not contain deleted platform
        var getResponse = await Client.GetAsync("api/catalog/platforms");
        var platforms = await getResponse.Content.ReadFromJsonAsync<IReadOnlyList<PlatformResponse>>();
        platforms.Should().NotContain(p => p.Id == platformId);

        // Assert 2 - Directly check DB with IgnoreQueryFilters to verify soft-delete
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShuffleSeries.Catalog.Infrastructure.Persistence.CatalogDbContext>();
        var deletedEntity = await dbContext.Platforms
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == platformId);

        deletedEntity.Should().NotBeNull();
        deletedEntity!.IsDeleted.Should().BeTrue();
        deletedEntity.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task DeletePlatform_WhenNotFound_ShouldReturn404()
    {
        // Act
        var deleteResponse = await Client.DeleteAsync($"api/catalog/platforms/{Guid.NewGuid()}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- MOODS ---

    [Fact]
    public async Task GetActiveMoods_WithContentNegotiation_ShouldReturnLocalizedMoods()
    {
        // Act
        var response = await Client.GetAsync("api/catalog/moods?lang=tr");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var moods = await response.Content.ReadFromJsonAsync<IReadOnlyList<MoodResponse>>();

        // Assert
        moods.Should().NotBeNull();
        moods!.Count.Should().BeGreaterThanOrEqualTo(9);
        moods.Should().Contain(m => m.Code == "beyin-yakanlar");
    }

    [Fact]
    public async Task CreateMood_ShouldPersist_AndInvalidateCache()
    {
        // Arrange
        var request = new CreateMoodRequest(
            Code: "korku-gerilim",
            AmbientColorHex: "#1a0000",
            Name: "Korku ve Gerilim",
            Description: "Tüyleri diken diken eden yapımlar.",
            LanguageCode: "tr",
            DisplayOrder: 20,
            IsActive: true);

        // Act
        var postResponse = await Client.PostAsJsonAsync("api/catalog/moods", request);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var moodId = await postResponse.Content.ReadFromJsonAsync<Guid>();
        moodId.Should().NotBeEmpty();

        // Assert - Cached list contains newly created mood
        var getResponse = await Client.GetAsync("api/catalog/moods?lang=tr");
        var moods = await getResponse.Content.ReadFromJsonAsync<IReadOnlyList<MoodResponse>>();
        moods.Should().Contain(m => m.Code == "korku-gerilim" && m.Name == "Korku ve Gerilim");
    }

    [Fact]
    public async Task DeleteMood_ShouldCascadeSoftDeleteMoodAndTranslations()
    {
        // Arrange - Create a mood with translation
        var request = new CreateMoodRequest(
            Code: "huzurlu-anlar",
            AmbientColorHex: "#00FF00",
            Name: "Huzurlu Anlar",
            Description: "Sakin ve dinlendirici içerikler.",
            LanguageCode: "tr");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/moods", request);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var moodId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Delete mood
        var deleteResponse = await Client.DeleteAsync($"api/catalog/moods/{moodId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert 1 - Active moods no longer contains deleted mood
        var getResponse = await Client.GetAsync("api/catalog/moods?lang=tr");
        var moods = await getResponse.Content.ReadFromJsonAsync<IReadOnlyList<MoodResponse>>();
        moods.Should().NotContain(m => m.Id == moodId);

        // Assert 2 - Directly inspect PostgreSQL DB with IgnoreQueryFilters to verify CASCADE soft-delete
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShuffleSeries.Catalog.Infrastructure.Persistence.CatalogDbContext>();

        var moodEntity = await dbContext.Moods
            .IgnoreQueryFilters()
            .Include(m => m.Translations)
            .FirstOrDefaultAsync(m => m.Id == moodId);

        moodEntity.Should().NotBeNull();
        moodEntity!.IsDeleted.Should().BeTrue();
        moodEntity.DeletedAtUtc.Should().NotBeNull();
        moodEntity.Translations.Should().NotBeEmpty();
        moodEntity.Translations.Should().AllSatisfy(t =>
        {
            t.IsDeleted.Should().BeTrue();
            t.DeletedAtUtc.Should().NotBeNull();
        });
    }

    // --- GENRES ---

    [Fact]
    public async Task GetActiveGenres_WithContentNegotiation_ShouldReturnLocalizedGenres()
    {
        // Act
        var response = await Client.GetAsync("api/catalog/genres?lang=tr");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var genres = await response.Content.ReadFromJsonAsync<IReadOnlyList<GenreResponse>>();

        // Assert
        genres.Should().NotBeNull();
        genres!.Count.Should().BeGreaterThanOrEqualTo(10);
        genres.Should().Contain(g => g.Code == "science-fiction");
    }

    [Fact]
    public async Task CreateGenre_ShouldPersist_AndInvalidateCache()
    {
        // Arrange
        var request = new CreateGenreRequest(
            Code: "anime",
            Name: "Anime",
            LanguageCode: "tr",
            Description: "Japon animasyon yapımları.",
            DisplayOrder: 30,
            IsActive: true);

        // Act
        var postResponse = await Client.PostAsJsonAsync("api/catalog/genres", request);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var genreId = await postResponse.Content.ReadFromJsonAsync<Guid>();
        genreId.Should().NotBeEmpty();

        // Assert - Cached list contains newly created genre
        var getResponse = await Client.GetAsync("api/catalog/genres?lang=tr");
        var genres = await getResponse.Content.ReadFromJsonAsync<IReadOnlyList<GenreResponse>>();
        genres.Should().Contain(g => g.Code == "anime" && g.Name == "Anime");
    }

    [Fact]
    public async Task DeleteGenre_ShouldCascadeSoftDeleteGenreAndTranslations()
    {
        // Arrange - Create a genre with translation
        var request = new CreateGenreRequest(
            Code: "cyberpunk",
            Name: "Cyberpunk",
            LanguageCode: "tr",
            Description: "Distopik gelecek ve ileri teknoloji temalı yapımlar.");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/genres", request);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var genreId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Delete genre
        var deleteResponse = await Client.DeleteAsync($"api/catalog/genres/{genreId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert 1 - Active genres no longer contains deleted genre
        var getResponse = await Client.GetAsync("api/catalog/genres?lang=tr");
        var genres = await getResponse.Content.ReadFromJsonAsync<IReadOnlyList<GenreResponse>>();
        genres.Should().NotContain(g => g.Id == genreId);

        // Assert 2 - Directly inspect PostgreSQL DB with IgnoreQueryFilters to verify CASCADE soft-delete
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShuffleSeries.Catalog.Infrastructure.Persistence.CatalogDbContext>();

        var genreEntity = await dbContext.Genres
            .IgnoreQueryFilters()
            .Include(g => g.Translations)
            .FirstOrDefaultAsync(g => g.Id == genreId);

        genreEntity.Should().NotBeNull();
        genreEntity!.IsDeleted.Should().BeTrue();
        genreEntity.DeletedAtUtc.Should().NotBeNull();
        genreEntity.Translations.Should().NotBeEmpty();
        genreEntity.Translations.Should().AllSatisfy(t =>
        {
            t.IsDeleted.Should().BeTrue();
            t.DeletedAtUtc.Should().NotBeNull();
        });
    }
}
