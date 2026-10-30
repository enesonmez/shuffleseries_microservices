using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Episodes;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Series;
using ShuffleSeries.Catalog.Application.Features.Episodes.Responses;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Catalog.IntegrationTests.Infrastructure;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Catalog.IntegrationTests.Endpoints;

public class EpisodeEndpointsIntegrationTests : CatalogIntegrationTestBase
{
    public EpisodeEndpointsIntegrationTests(CatalogApiFactory factory) : base(factory)
    {
    }

    private async Task<Guid> CreateSampleSeriesAsync()
    {
        var request = new CreateSeriesRequest(
            TmdbId: Random.Shared.Next(100000, 999999),
            ImdbId: $"tt{Random.Shared.Next(1000000, 9999999)}",
            ReleaseYear: 2008,
            SeriesType: SeriesType.Standard,
            PosterUrl: "https://example.com/bb.jpg",
            BackdropUrl: null,
            LanguageCode: "tr",
            Title: $"Breaking Bad {Guid.NewGuid():N}",
            Overview: "Kimya öğretmeni ve uyuşturucu dünyası.",
            TotalSeasons: 5);

        var response = await Client.PostAsJsonAsync("api/catalog/series", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task CreateEpisode_WithValidPayload_ShouldPersistInRealPostgreSql_AndReturn201Created()
    {
        // Arrange
        var seriesId = await CreateSampleSeriesAsync();
        var request = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 1,
            Title: "Pilot",
            Overview: "Walter White teşhis alır.",
            LanguageCode: "tr",
            IsStandalone: false,
            RuntimeMinutes: 58,
            ImdbRating: 9.0,
            ThumbnailUrl: "https://example.com/bb-s1e1.jpg");

        // Act
        var response = await Client.PostAsJsonAsync("api/catalog/episodes", request);

        // Assert HTTP
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var episodeId = await response.Content.ReadFromJsonAsync<Guid>();
        episodeId.Should().NotBeEmpty();

        // Assert DB
        await ExecuteDbContextAsync(async context =>
        {
            var episode = await context.Episodes
                .Include(e => e.Translations)
                .FirstOrDefaultAsync(e => e.Id == episodeId);

            episode.Should().NotBeNull();
            episode!.EpisodeNumber.Should().Be(1);
            episode.RuntimeMinutes.Should().Be(58);
            episode.ImdbRating.Should().Be(9.0);

            var season = await context.Seasons.FindAsync(episode.SeasonId);
            season.Should().NotBeNull();
            season!.SeasonNumber.Should().Be(1);
            season.SeriesId.Should().Be(seriesId);
            episode.Translations.Should().ContainSingle(t => t.LanguageCode == "tr" && t.Title == "Pilot");
        });
    }

    [Fact]
    public async Task GetEpisodeById_WithContentNegotiation_ShouldReturnLocalizedDetails()
    {
        // Arrange
        var seriesId = await CreateSampleSeriesAsync();
        var request = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 2,
            Title: "Kedi Kutuda",
            Overview: "Türkçe özet",
            LanguageCode: "tr");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/episodes", request);
        var episodeId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await Client.GetAsync($"api/catalog/episodes/{episodeId}?lang=tr");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var episodeDetail = await response.Content.ReadFromJsonAsync<EpisodeDetailResponse>();
        episodeDetail.Should().NotBeNull();
        episodeDetail!.Title.Should().Be("Kedi Kutuda");
        episodeDetail.EpisodeNumber.Should().Be(2);
        episodeDetail.SeasonId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateEpisode_WithValidPayload_ShouldUpdateInPostgreSql_AndReturn204NoContent()
    {
        // Arrange
        var seriesId = await CreateSampleSeriesAsync();
        var createRequest = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 3,
            Title: "Ve Çanta Dolu",
            Overview: "Özet",
            LanguageCode: "tr");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/episodes", createRequest);
        var episodeId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Update
        var updateRequest = new UpdateEpisodeRequest(
            EpisodeNumber: 3,
            IsStandalone: true,
            RuntimeMinutes: 50,
            ImdbRating: 8.8,
            ThumbnailUrl: "https://example.com/updated-thumb.jpg");

        var updateResponse = await Client.PutAsJsonAsync($"api/catalog/episodes/{episodeId}", updateRequest);

        // Assert HTTP
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB
        await ExecuteDbContextAsync(async context =>
        {
            var episode = await context.Episodes.FindAsync(episodeId);
            episode.Should().NotBeNull();
            episode!.IsStandalone.Should().BeTrue();
            episode.RuntimeMinutes.Should().Be(50);
            episode.ImdbRating.Should().Be(8.8);
            episode.ThumbnailUrl.Should().Be("https://example.com/updated-thumb.jpg");
        });
    }

    [Fact]
    public async Task DeleteEpisode_ShouldSoftDeleteInPostgreSql_AndReturn204NoContent()
    {
        // Arrange
        var seriesId = await CreateSampleSeriesAsync();
        var createRequest = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 4,
            Title: "Kanser Adam",
            Overview: "Özet",
            LanguageCode: "tr");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/episodes", createRequest);
        var episodeId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var deleteResponse = await Client.DeleteAsync($"api/catalog/episodes/{episodeId}");

        // Assert HTTP
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert GET returns 404
        var getResponse = await Client.GetAsync($"api/catalog/episodes/{episodeId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Assert DB is soft-deleted
        await ExecuteDbContextAsync(async context =>
        {
            var episode = await context.Episodes.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == episodeId);
            episode.Should().NotBeNull();
            episode!.IsDeleted.Should().BeTrue();
        });
    }

    [Fact]
    public async Task AddAndRemoveEpisodePlatform_ShouldModifyAssociationsInPostgreSql()
    {
        // Arrange
        var seriesId = await CreateSampleSeriesAsync();
        var createRequest = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 5,
            Title: "Bozkır",
            Overview: "Özet",
            LanguageCode: "tr");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/episodes", createRequest);
        var episodeId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        Guid platformId = Guid.Empty;
        await ExecuteDbContextAsync(async context =>
        {
            platformId = (await context.Platforms.FirstAsync(p => p.Slug == "netflix")).Id;
        });

        var addRequest = new AddEpisodePlatformRequest(platformId, "https://netflix.com/watch/555", "nflx://watch/555");

        // Act 1: Add episode platform
        var addResponse = await Client.PostAsJsonAsync($"api/catalog/episodes/{episodeId}/platforms", addRequest);
        addResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB has platform
        await ExecuteDbContextAsync(async context =>
        {
            var ep = await context.EpisodePlatforms.FirstOrDefaultAsync(x => x.EpisodeId == episodeId && x.PlatformId == platformId);
            ep.Should().NotBeNull();
            ep!.WebUrl.Should().Be("https://netflix.com/watch/555");
            ep.AppSchemeUrl.Should().Be("nflx://watch/555");
        });

        // Act 2: Remove episode platform
        var removeResponse = await Client.DeleteAsync($"api/catalog/episodes/{episodeId}/platforms/{platformId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB platform is removed
        await ExecuteDbContextAsync(async context =>
        {
            var ep = await context.EpisodePlatforms.FirstOrDefaultAsync(x => x.EpisodeId == episodeId && x.PlatformId == platformId);
            ep.Should().BeNull();
        });
    }

    [Fact]
    public async Task AddOrUpdateEpisodeTranslation_AndRemoveEpisodeTranslation_ShouldModifyInPostgreSql()
    {
        // Arrange
        var seriesId = await CreateSampleSeriesAsync();
        var createRequest = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 6,
            Title: "Türkçe Başlık",
            Overview: "Türkçe Açıklama",
            LanguageCode: "tr");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/episodes", createRequest);
        var episodeId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        var addTransRequest = new AddOrUpdateEpisodeTranslationRequest("en", "English Title", "English Overview");

        // Act 1: Add EN translation
        var addResponse = await Client.PostAsJsonAsync($"api/catalog/episodes/{episodeId}/translations", addTransRequest);
        addResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB has both translations
        await ExecuteDbContextAsync(async context =>
        {
            var enTrans = await context.EpisodeTranslations
                .FirstOrDefaultAsync(x => x.EpisodeId == episodeId && x.LanguageCode == "en");
            enTrans.Should().NotBeNull();
            enTrans!.Title.Should().Be("English Title");
        });

        // Act 2: Remove EN translation
        var removeResponse = await Client.DeleteAsync($"api/catalog/episodes/{episodeId}/translations/en");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB translation is removed
        await ExecuteDbContextAsync(async context =>
        {
            var enTrans = await context.EpisodeTranslations
                .FirstOrDefaultAsync(x => x.EpisodeId == episodeId && x.LanguageCode == "en");
            enTrans.Should().BeNull();
        });
    }

    [Fact]
    public async Task AddEpisodePlatform_WhenCalledByNonAdmin_ShouldReturn403Forbidden()
    {
        // Arrange
        var seriesId = await CreateSampleSeriesAsync();
        var createRequest = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 7,
            Title: "Test Episode",
            Overview: "Overview",
            LanguageCode: "tr");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/episodes", createRequest);
        var episodeId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Standart kullanıcı istemcisi (Admin rolü veya catalog:associations:manage izni YOK)
        var nonAdminClient = CreateAuthorizedClient(
            role: SystemRoles.Standard,
            permissions: [SystemPermissions.Catalog.EpisodesRead]);

        var addRequest = new AddEpisodePlatformRequest(Guid.NewGuid(), "https://netflix.com/watch/999");

        // Act
        var response = await nonAdminClient.PostAsJsonAsync($"api/catalog/episodes/{episodeId}/platforms", addRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateEpisode_WithInvalidPayload_ShouldReturn400BadRequest()
    {
        // Arrange
        var seriesId = await CreateSampleSeriesAsync();
        var invalidRequest = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 0, // Geçersiz (<= 0)
            EpisodeNumber: -1, // Geçersiz (<= 0)
            Title: "", // Boş başlık
            Overview: "Overview",
            LanguageCode: "tr",
            RuntimeMinutes: -10, // Geçersiz
            ImdbRating: 11.5); // Geçersiz (> 10.0)

        // Act
        var response = await Client.PostAsJsonAsync("api/catalog/episodes", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEpisode_WhenSeriesDoesNotExist_ShouldReturn404NotFound()
    {
        // Arrange
        var nonExistentSeriesId = Guid.NewGuid();
        var request = new CreateEpisodeRequest(
            SeriesId: nonExistentSeriesId,
            SeasonNumber: 1,
            EpisodeNumber: 1,
            Title: "Ghost Episode",
            Overview: "No series",
            LanguageCode: "tr");

        // Act
        var response = await Client.PostAsJsonAsync("api/catalog/episodes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateEpisode_WhenEpisodeNumberAlreadyExistsInSameSeason_ShouldReturn409Conflict()
    {
        // Arrange
        var seriesId = await CreateSampleSeriesAsync();
        var request1 = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 1,
            Title: "Episode 1 First",
            Overview: "First",
            LanguageCode: "tr");

        var response1 = await Client.PostAsJsonAsync("api/catalog/episodes", request1);
        response1.EnsureSuccessStatusCode();

        var request2 = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 1, // Aynı sezon ve aynı bölüm no
            Title: "Episode 1 Duplicate",
            Overview: "Duplicate",
            LanguageCode: "tr");

        // Act
        var response2 = await Client.PostAsJsonAsync("api/catalog/episodes", request2);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetEpisodeById_WhenDoesNotExist_ShouldReturn404NotFound()
    {
        // Act
        var response = await Client.GetAsync($"api/catalog/episodes/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateEpisode_WhenDoesNotExist_ShouldReturn404NotFound()
    {
        // Arrange
        var updateRequest = new UpdateEpisodeRequest(
            EpisodeNumber: 1,
            IsStandalone: true);

        // Act
        var response = await Client.PutAsJsonAsync($"api/catalog/episodes/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEpisode_WhenDoesNotExist_ShouldReturn404NotFound()
    {
        // Act
        var response = await Client.DeleteAsync($"api/catalog/episodes/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStandaloneEpisodes_FromRepository_ShouldReturnOnlyStandaloneEpisodesOrderedByRating()
    {
        // Arrange
        var seriesId = await CreateSampleSeriesAsync();

        var ep1Request = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 1,
            Title: "Standalone Low",
            Overview: "Overview 1",
            LanguageCode: "tr",
            IsStandalone: true,
            RuntimeMinutes: 45,
            ImdbRating: 7.5);
        var resp1 = await Client.PostAsJsonAsync("api/catalog/episodes", ep1Request);
        var ep1Id = await resp1.Content.ReadFromJsonAsync<Guid>();

        var ep2Request = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 2,
            Title: "Not Standalone High",
            Overview: "Overview 2",
            LanguageCode: "tr",
            IsStandalone: false,
            RuntimeMinutes: 45,
            ImdbRating: 9.8);
        var resp2 = await Client.PostAsJsonAsync("api/catalog/episodes", ep2Request);
        var ep2Id = await resp2.Content.ReadFromJsonAsync<Guid>();

        var ep3Request = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 3,
            Title: "Standalone High",
            Overview: "Overview 3",
            LanguageCode: "tr",
            IsStandalone: true,
            RuntimeMinutes: 50,
            ImdbRating: 9.2);
        var resp3 = await Client.PostAsJsonAsync("api/catalog/episodes", ep3Request);
        var ep3Id = await resp3.Content.ReadFromJsonAsync<Guid>();

        // Act
        using var scope = Factory.Services.CreateScope();
        var episodeRepo = scope.ServiceProvider.GetRequiredService<IEpisodeRepository>();
        var standaloneEpisodes = await episodeRepo.GetStandaloneEpisodesAsync(limit: 50);

        // Assert
        standaloneEpisodes.Should().NotBeEmpty();
        var matchingEpisodes = standaloneEpisodes.Where(e => e.Id == ep1Id || e.Id == ep2Id || e.Id == ep3Id).ToList();
        matchingEpisodes.Should().HaveCount(2);
        matchingEpisodes.Should().OnlyContain(e => e.IsStandalone);
        matchingEpisodes[0].Id.Should().Be(ep3Id); // 9.2 rating comes before 7.5
        matchingEpisodes[1].Id.Should().Be(ep1Id); // 7.5 rating
    }

    [Fact]
    public async Task AddEpisodePlatform_WhenAlreadyAssociated_ShouldReturn409Conflict_AndRemoveShouldModifyInPostgreSql()
    {
        // Arrange
        var seriesId = await CreateSampleSeriesAsync();
        var createRequest = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 8,
            Title: "Platform Test Episode",
            Overview: "Overview",
            LanguageCode: "tr");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/episodes", createRequest);
        var episodeId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Create Platform
        var platformId = Guid.NewGuid();
        await ExecuteDbContextAsync(async context =>
        {
            var p = Platform.Create("Netflix Test", "netflix-test", "https://logo.png", "https://off.png", "https://netflix.com");
            context.Platforms.Add(p);
            await context.SaveChangesAsync();
            platformId = p.Id;
        });

        // 1. Add Platform (Admin - 204)
        var addRequest = new AddEpisodePlatformRequest(platformId, "https://netflix.com/watch/101", "nflx://watch/101");
        var addResponse = await Client.PostAsJsonAsync($"api/catalog/episodes/{episodeId}/platforms", addRequest);
        addResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert added in DB
        await ExecuteDbContextAsync(async context =>
        {
            var epPlatform = await context.EpisodePlatforms
                .FirstOrDefaultAsync(x => x.EpisodeId == episodeId && x.PlatformId == platformId);
            epPlatform.Should().NotBeNull();
            epPlatform!.WebUrl.Should().Be("https://netflix.com/watch/101");
            epPlatform.AppSchemeUrl.Should().Be("nflx://watch/101");
        });

        // 2. Try to add same Platform again -> 409 Conflict (Duplicate Protection)
        var duplicateRequest = new AddEpisodePlatformRequest(platformId, "https://netflix.com/watch/102", "nflx://watch/102");
        var duplicateResponse = await Client.PostAsJsonAsync($"api/catalog/episodes/{episodeId}/platforms", duplicateRequest);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Assert unchanged in DB
        await ExecuteDbContextAsync(async context =>
        {
            var platforms = await context.EpisodePlatforms
                .Where(x => x.EpisodeId == episodeId && x.PlatformId == platformId)
                .ToListAsync();
            platforms.Should().HaveCount(1);
            platforms[0].WebUrl.Should().Be("https://netflix.com/watch/101");
            platforms[0].AppSchemeUrl.Should().Be("nflx://watch/101");
        });

        // 3. Remove Platform (204)
        var removeResponse = await Client.DeleteAsync($"api/catalog/episodes/{episodeId}/platforms/{platformId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert removed from DB
        await ExecuteDbContextAsync(async context =>
        {
            var epPlatform = await context.EpisodePlatforms
                .FirstOrDefaultAsync(x => x.EpisodeId == episodeId && x.PlatformId == platformId);
            epPlatform.Should().BeNull();
        });
    }

    [Fact]
    public async Task EpisodeOperations_ShouldProduceOutboxMessagesInPostgreSql()
    {
        // 1. Arrange & Create Series
        var seriesId = await CreateSampleSeriesAsync();

        // 2. Create Episode -> Should produce EpisodeCreatedDomainEvent and MediaUpdatedDomainEvent
        var createRequest = new CreateEpisodeRequest(
            SeriesId: seriesId,
            SeasonNumber: 1,
            EpisodeNumber: 99,
            Title: "Outbox Test Episode",
            Overview: "Testing outbox events",
            LanguageCode: "tr",
            IsStandalone: true,
            RuntimeMinutes: 50,
            ImdbRating: 9.0);

        var postResponse = await Client.PostAsJsonAsync("api/catalog/episodes", createRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var episodeId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        await ExecuteDbContextAsync(async context =>
        {
            var outboxMessages = await context.OutboxMessages.ToListAsync();
            outboxMessages.Should().Contain(m => m.Type == "EpisodeCreatedDomainEvent");
        });

        // 3. Update Episode -> Should produce EpisodeUpdatedDomainEvent
        var updateRequest = new UpdateEpisodeRequest(
            EpisodeNumber: 99,
            IsStandalone: true,
            RuntimeMinutes: 55,
            ImdbRating: 9.2);

        var putResponse = await Client.PutAsJsonAsync($"api/catalog/episodes/{episodeId}", updateRequest);
        putResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await ExecuteDbContextAsync(async context =>
        {
            var outboxMessages = await context.OutboxMessages.ToListAsync();
            outboxMessages.Should().Contain(m => m.Type == "EpisodeUpdatedDomainEvent");
        });

        // 4. Delete Episode -> Should produce EpisodeDeletedDomainEvent
        var deleteResponse = await Client.DeleteAsync($"api/catalog/episodes/{episodeId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await ExecuteDbContextAsync(async context =>
        {
            var outboxMessages = await context.OutboxMessages.ToListAsync();
            outboxMessages.Should().Contain(m => m.Type == "EpisodeDeletedDomainEvent");
        });
    }
}
