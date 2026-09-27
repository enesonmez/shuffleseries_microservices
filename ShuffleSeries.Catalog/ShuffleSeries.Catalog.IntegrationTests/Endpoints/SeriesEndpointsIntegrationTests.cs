using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;
using ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesById;
using ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesList;
using ShuffleSeries.Catalog.IntegrationTests.Infrastructure;
using ShuffleSeries.Shared.Core.Application.Responses;

namespace ShuffleSeries.Catalog.IntegrationTests.Endpoints;

public class SeriesEndpointsIntegrationTests : CatalogIntegrationTestBase
{
    public SeriesEndpointsIntegrationTests(CatalogApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateSeries_WithValidPayload_ShouldPersistInRealPostgreSql_AndReturn201Created()
    {
        // Arrange
        var command = new CreateSeriesCommand("Severance", "Lumos mystery sci-fi series", false);

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
            var seriesInDb = await context.Series.FirstOrDefaultAsync(s => s.Id == createdId);
            seriesInDb.Should().NotBeNull();
            seriesInDb!.Title.Should().Be("Severance");
            seriesInDb.Description.Should().Be("Lumos mystery sci-fi series");
            seriesInDb.IsDeleted.Should().BeFalse();

            // Outbox mesajının da gerçek veritabanına yazıldığını doğrula (PostgreSQL jsonb uyumlu)
            var outboxMessage = await context.OutboxMessages
                .FirstOrDefaultAsync(m => m.Type.Contains("SeriesCreatedDomainEvent"));
            outboxMessage.Should().NotBeNull();
            outboxMessage!.Content.Should().Contain(createdId.ToString());
        });
    }

    [Fact]
    public async Task CreateSeries_WithDuplicateTitle_ShouldReturn422UnprocessableEntity()
    {
        // Arrange
        var command = new CreateSeriesCommand("Breaking Bad", "Chemistry teacher turns kingpin", false);
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
        var createCommand = new CreateSeriesCommand("Dark", "Winden time travel puzzle", false);
        var createResponse = await Client.PostAsJsonAsync("api/catalog/series", createCommand);
        var seriesId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var getResponse = await Client.GetAsync($"api/catalog/series/{seriesId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var seriesDto = await getResponse.Content.ReadFromJsonAsync<SeriesResponse>();
        seriesDto.Should().NotBeNull();
        seriesDto!.Id.Should().Be(seriesId);
        seriesDto.Title.Should().Be("Dark");
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
        var createCommand = new CreateSeriesCommand("Mindhunter", "FBI behavioral science unit", false);
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
            var command = new CreateSeriesCommand($"Series {i}", $"Description {i}", false);
            await Client.PostAsJsonAsync("api/catalog/series", command);
        }

        // Act (1. Sayfa, 2 öğe)
        var response = await Client.GetAsync("api/catalog/series?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paginatedResult = await response.Content.ReadFromJsonAsync<PaginatedList<SeriesListItemResponse>>();
        paginatedResult.Should().NotBeNull();
        paginatedResult!.Items.Should().HaveCount(2);
        paginatedResult.TotalCount.Should().Be(3);
        paginatedResult.TotalPages.Should().Be(2);
        paginatedResult.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task CreateSeries_WithEmptyTitle_ShouldReturn400BadRequest_WhenValidationFails()
    {
        // Arrange
        var command = new CreateSeriesCommand("", "Description", false);

        // Act
        var response = await Client.PostAsJsonAsync("api/catalog/series", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSeries_WithValidPayload_ShouldUpdateInPostgreSql_AndReturn204NoContent()
    {
        // Arrange
        var createCommand = new CreateSeriesCommand("Succession", "Media dynasty satire", false);
        var createResponse = await Client.PostAsJsonAsync("api/catalog/series", createCommand);
        var seriesId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var updateCommand = new UpdateSeriesCommand(seriesId, "Succession: Final Season", "Roy family war", true);

        // Act
        var updateResponse = await Client.PutAsJsonAsync($"api/catalog/series/{seriesId}", updateCommand);

        // Assert HTTP
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert Database State
        await ExecuteDbContextAsync(async context =>
        {
            var updated = await context.Series.FirstOrDefaultAsync(s => s.Id == seriesId);
            updated.Should().NotBeNull();
            updated!.Title.Should().Be("Succession: Final Season");
            updated.Description.Should().Be("Roy family war");
            updated.IsIndependentEpisodes.Should().BeTrue();
            updated.ModifiedAtUtc.Should().NotBeNull();

            // Outbox mesajının yazıldığını doğrula
            var outboxMessage = await context.OutboxMessages
                .FirstOrDefaultAsync(m => m.Type.Contains("SeriesUpdatedDomainEvent"));
            outboxMessage.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task UpdateSeries_WhenIdMismatch_ShouldReturn400BadRequest()
    {
        // Arrange
        var routeId = Guid.NewGuid();
        var bodyId = Guid.NewGuid();
        var command = new UpdateSeriesCommand(bodyId, "Title", "Desc", false);

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
        var command = new UpdateSeriesCommand(nonExistentId, "Non Existent", "Desc", false);

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

