using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.IntegrationTests.Infrastructure;

namespace ShuffleSeries.Catalog.IntegrationTests.Endpoints;

public class RealInfrastructureIntegrationTests : CatalogIntegrationTestBase
{
    public RealInfrastructureIntegrationTests(CatalogApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RedisCache_ShouldPersistTaxonomy_AndFlushOnReset()
    {
        // 1. Arrange & Act: Moods listesi endpoint'ini çağır (Cache-Aside: DB -> Redis)
        var response = await Client.GetAsync("/api/catalog/moods");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Assert: Gerçek Redis container'ında cache anahtarının oluştuğunu doğrula
        var redisDb = Factory.Redis.GetDatabase();
        var cachedMoods = await redisDb.StringGetAsync("shuffleseries:catalog:metadata:moods");
        cachedMoods.HasValue.Should().BeTrue();
        cachedMoods.ToString().Should().Contain("beyin-yakanlar");

        // 3. ResetState: Test arası izolasyon ile Redis'in temizlendiğini doğrula
        await Factory.ResetStateAsync();
        var flushedMoods = await redisDb.StringGetAsync("shuffleseries:catalog:metadata:moods");
        flushedMoods.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task OutboxJob_ShouldPublishEventToRealRabbitMQ_AndMarkProcessedInRealPostgres()
    {
        // 1. Arrange: Geçerli bir dizi oluştur (PostgreSQL OutboxMessages tablosuna event yazılır)
        var createCommand = new CreateSeriesCommand(
            TmdbId: 199999,
            ImdbId: "tt19999999",
            ReleaseYear: 2024,
            SeriesType: SeriesType.Standard,
            PosterUrl: "https://example.com/dark-matter.jpg",
            BackdropUrl: "https://example.com/dark-matter-bg.jpg",
            LanguageCode: "tr",
            Title: "Dark Matter Test",
            Overview: "Sci-fi thriller series for outbox testing",
            Tagline: "Are you happy with your life?",
            TotalSeasons: 1);

        var createResponse = await Client.PostAsJsonAsync("/api/catalog/series", createCommand);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Assert: Outbox tablosunda henüz işlenmemiş mesajın varlığını doğrula
        await ExecuteDbContextAsync(async db =>
        {
            var pendingMessage = await db.OutboxMessages
                .FirstOrDefaultAsync(m => m.ProcessedOnUtc == null && m.Type.Contains("MediaCreatedDomainEvent"));

            pendingMessage.Should().NotBeNull();
            pendingMessage!.ProcessedOnUtc.Should().BeNull();
            pendingMessage.Error.Should().BeNull();
        });

        // 3. Act: Outbox job'ını deterministik olarak on-demand tetikle
        // (Gerçek Testcontainers RabbitMQ'ya mesaj publish edilir ve PostgreSQL güncellenir)
        await TriggerOutboxJobAsync();

        // 4. Assert: Mesajın başarıyla işlendiğini ve ProcessedOnUtc'nin dolduğunu doğrula
        await ExecuteDbContextAsync(async db =>
        {
            var processedMessage = await db.OutboxMessages
                .FirstOrDefaultAsync(m => m.Type.Contains("MediaCreatedDomainEvent"));

            processedMessage.Should().NotBeNull();
            processedMessage!.ProcessedOnUtc.Should().NotBeNull();
            processedMessage.Error.Should().BeNull();
            processedMessage.RetryCount.Should().Be(0);
        });
    }
}
