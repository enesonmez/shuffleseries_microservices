using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;
using ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesList;
using ShuffleSeries.Catalog.Application.Features.Series.Responses;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.IntegrationTests.Infrastructure;
using ShuffleSeries.Shared.Core.Application.Responses;

namespace ShuffleSeries.Catalog.IntegrationTests.Endpoints;

public class SeriesEndpointsIntegrationTests : CatalogIntegrationTestBase
{
    public SeriesEndpointsIntegrationTests(CatalogApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Database_ShouldContainSeededMoodsAndPlatforms_OnStartup()
    {
        await ExecuteDbContextAsync(async context =>
        {
            var moods = await context.Moods.Include(m => m.Translations).ToListAsync();
            moods.Count.Should().BeGreaterThanOrEqualTo(9);
            moods.Should().Contain(m => m.Code == "beyin-yakanlar");
            moods.Should().Contain(m => m.Code == "kafa-dagitmalik");
            moods.Should().Contain(m => m.Code == "icini-isitanlar");
            moods.Should().Contain(m => m.Code == "zeka-oyunlari");
            moods.Should().Contain(m => m.Code == "ufkunu-genislet");

            var platforms = await context.Platforms.ToListAsync();
            platforms.Count.Should().BeGreaterThanOrEqualTo(5);
            platforms.Should().Contain(p => p.Slug == "netflix");
            platforms.Should().Contain(p => p.Slug == "prime-video");
        });
    }

    [Fact]
    public async Task CreateSeries_WithValidPayload_ShouldPersistInRealPostgreSql_AndReturn201Created()
    {
        // Arrange
        var command = new CreateSeriesCommand(
            ReleaseYear: 2022,
            SeriesType: SeriesType.Standard,
            PosterUrl: "https://example.com/severance.jpg",
            BackdropUrl: "https://example.com/severance-bg.jpg",
            LanguageCode: "tr",
            Title: "Severance",
            Overview: "Lumos mystery sci-fi series",
            Tagline: "Please do not attempt to leave",
            ImdbId: "tt11280740",
            ImdbRating: 8.7,
            ImdbVotes: 180000,
            TotalSeasons: 2);

        // Act
        var response = await Client.PostAsJsonAsync("api/catalog/series", command);

        // Assert HTTP Response
        var contentString = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: contentString);
        response.Headers.Location.Should().NotBeNull();

        var createdId = await response.Content.ReadFromJsonAsync<Guid>();
        createdId.Should().NotBeEmpty();

        // Assert Database State (Gerçek PostgreSQL sorgusu)
        await ExecuteDbContextAsync(async context =>
        {
            var seriesInDb = await context.Series
                .Include(s => s.Translations)
                .FirstOrDefaultAsync(s => s.Id == createdId);

            seriesInDb.Should().NotBeNull();
            seriesInDb!.ReleaseYear.Should().Be(2022);
            seriesInDb.SeriesType.Should().Be(SeriesType.Standard);
            seriesInDb.IsDeleted.Should().BeFalse();
            seriesInDb.Translations.Should().ContainSingle(t => t.Title == "Severance");
            seriesInDb.Translations.First().Overview.Should().Be("Lumos mystery sci-fi series");

            // Outbox mesajının da gerçek veritabanına yazıldığını doğrula (PostgreSQL jsonb uyumlu)
            var messages = await context.OutboxMessages
                .Where(m => m.Type.Contains("MediaCreatedDomainEvent"))
                .ToListAsync();
            var outboxMessage = messages.FirstOrDefault(m => m.Content.Contains(createdId.ToString()));
            outboxMessage.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task CreateSeries_WithDuplicateTitle_ShouldReturn422UnprocessableEntity()
    {
        // Arrange
        var command = new CreateSeriesCommand(
            2008,
            SeriesType.Standard,
            "https://example.com/bb.jpg",
            null,
            "tr",
            "Breaking Bad",
            "Chemistry teacher turns kingpin");

        var firstResponse = await Client.PostAsJsonAsync("api/catalog/series", command);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act (Aynı başlıkla ikinci kayıt)
        var secondResponse = await Client.PostAsJsonAsync("api/catalog/series", command);

        // Assert (BusinessException -> 422 Unprocessable Entity)
        secondResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetSeriesById_WhenSeriesExists_ShouldReturn200Ok_AndCorrectData()
    {
        // Arrange
        var createCommand = new CreateSeriesCommand(
            2017,
            SeriesType.Standard,
            "https://example.com/dark.jpg",
            null,
            "tr",
            "Dark",
            "Winden time travel puzzle");

        var createResponse = await Client.PostAsJsonAsync("api/catalog/series", createCommand);
        var seriesId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var getResponse = await Client.GetAsync($"api/catalog/series/{seriesId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var seriesDto = await getResponse.Content.ReadFromJsonAsync<SeriesResponse>();
        seriesDto.Should().NotBeNull();
        seriesDto!.Id.Should().Be(seriesId);
        seriesDto.Translations.Should().ContainSingle(t => t.Title == "Dark");
    }

    [Fact]
    public async Task GetSeriesById_WhenNotExists_ShouldReturn404NotFound()
    {
        // Act
        var response = await Client.GetAsync($"api/catalog/series/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSeries_ShouldSoftDeleteInPostgreSql_AndHideFromGetEndpoint()
    {
        // Arrange
        var createCommand = new CreateSeriesCommand(
            2017,
            SeriesType.Standard,
            "https://example.com/mindhunter.jpg",
            null,
            "tr",
            "Mindhunter",
            "FBI behavioral science unit");

        var createResponse = await Client.PostAsJsonAsync("api/catalog/series", createCommand);
        var seriesId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act (Soft delete isteği)
        var deleteResponse = await Client.DeleteAsync($"api/catalog/series/{seriesId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert 1: Normal API üzerinden sorgulanamamalı (Global Query Filter)
        var getResponse = await Client.GetAsync($"api/catalog/series/{seriesId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Assert 2: Veritabanında fiziksel olarak silinmemiş, IsDeleted=true ve DeletedAtUtc dolu olmalı
        await ExecuteDbContextAsync(async context =>
        {
            var rawEntity = await context.Series
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == seriesId);

            rawEntity.Should().NotBeNull();
            rawEntity!.IsDeleted.Should().BeTrue();
            rawEntity.DeletedAtUtc.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task GetSeriesList_ShouldReturnPaginatedResults_FromRealPostgreSql()
    {
        // Arrange
        for (var i = 1; i <= 3; i++)
        {
            var command = new CreateSeriesCommand(
                2020 + i,
                SeriesType.Standard,
                $"https://example.com/poster{i}.jpg",
                null,
                "tr",
                $"Integration Series {i}",
                $"Integration Description {i}");

            await Client.PostAsJsonAsync("api/catalog/series", command);
        }

        // Act (1. Sayfa, 2 öğe)
        var response = await Client.GetAsync("api/catalog/series?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paginatedResult = await response.Content.ReadFromJsonAsync<PaginatedList<SeriesListItemResponse>>();
        paginatedResult.Should().NotBeNull();
        paginatedResult!.Items.Should().HaveCount(2);
        paginatedResult.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task CreateSeries_WithEmptyTitle_ShouldReturn400BadRequest_WhenValidationFails()
    {
        // Arrange
        var command = new CreateSeriesCommand(
            2020,
            SeriesType.Standard,
            "https://example.com/poster.jpg",
            null,
            "tr",
            "",
            "Valid Description");

        // Act
        var response = await Client.PostAsJsonAsync("api/catalog/series", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSeries_WithValidPayload_ShouldUpdateInPostgreSql_AndReturn204NoContent()
    {
        // Arrange
        var createCommand = new CreateSeriesCommand(
            2018,
            SeriesType.Standard,
            "https://example.com/succession.jpg",
            null,
            "tr",
            "Succession",
            "Media dynasty satire");

        var createResponse = await Client.PostAsJsonAsync("api/catalog/series", createCommand);
        var seriesId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var updateCommand = new UpdateSeriesCommand(
            seriesId,
            2019,
            SeriesType.Standard,
            4,
            "tt7660850",
            8.9,
            200000);

        // Act
        var updateResponse = await Client.PutAsJsonAsync($"api/catalog/series/{seriesId}", updateCommand);

        // Assert HTTP
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert Database State
        await ExecuteDbContextAsync(async context =>
        {
            var updated = await context.Series.FirstOrDefaultAsync(s => s.Id == seriesId);
            updated.Should().NotBeNull();
            updated!.ReleaseYear.Should().Be(2019);
            updated.TotalSeasons.Should().Be(4);
            updated.ImdbId.Should().Be("tt7660850");
            updated.ImdbRating.Should().Be(8.9);
            updated.ModifiedAtUtc.Should().NotBeNull();

            // Outbox mesajının yazıldığını doğrula
            var updateMessages = await context.OutboxMessages
                .Where(m => m.Type.Contains("MediaUpdatedDomainEvent"))
                .ToListAsync();
            var outboxMessage = updateMessages.FirstOrDefault(m => m.Content.Contains(seriesId.ToString()));
            outboxMessage.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task UpdateSeries_WhenIdMismatch_ShouldReturn400BadRequest()
    {
        // Arrange
        var routeId = Guid.NewGuid();
        var bodyId = Guid.NewGuid();
        var command = new UpdateSeriesCommand(bodyId, 2020, SeriesType.Standard, 1);

        // Act
        var response = await Client.PutAsJsonAsync($"api/catalog/series/{routeId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSeries_WhenNotFound_ShouldReturn404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var command = new UpdateSeriesCommand(nonExistentId, 2020, SeriesType.Standard, 1);

        // Act
        var response = await Client.PutAsJsonAsync($"api/catalog/series/{nonExistentId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSeries_WhenNotFound_ShouldReturn404NotFound()
    {
        // Act
        var response = await Client.DeleteAsync($"api/catalog/series/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSeriesList_WithZeroOrNegativePage_ShouldNormalizeToPageOne_AndReturn200Ok()
    {
        // Act (Negatif veya 0 sayfa numarası gönderildiğinde mimarimiz güvenle Page=1'e normalize eder)
        var response = await Client.GetAsync("api/catalog/series?page=0&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<SeriesListItemResponse>>();
        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(1);
    }
}
