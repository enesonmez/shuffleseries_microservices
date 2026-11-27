using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Api.Grpc;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.IntegrationTests.Infrastructure;

namespace ShuffleSeries.Catalog.IntegrationTests.Grpc;

public class CatalogGrpcServiceIntegrationTests : CatalogIntegrationTestBase
{
    public CatalogGrpcServiceIntegrationTests(CatalogApiFactory factory) : base(factory)
    {
    }

    private CatalogGrpc.CatalogGrpcClient CreateGrpcClient(out GrpcChannel channel)
    {
        channel = GrpcChannel.ForAddress(Factory.Server.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = Factory.Server.CreateHandler()
        });

        return new CatalogGrpc.CatalogGrpcClient(channel);
    }

    [Fact]
    public async Task GetBulkMediaForShuffle_Should_ReturnAllSeededMedia_WithMultilingualTranslationsAndTaxonomies()
    {
        var client = CreateGrpcClient(out var channel);
        using var _ = channel;

        var response = await client.GetBulkMediaForShuffleAsync(new GetBulkMediaForShuffleRequest
        {
            PageNumber = 1,
            PageSize = 50
        });

        response.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        response.PageNumber.Should().Be(1);
        response.PageSize.Should().Be(50);
        response.Items.Should().Contain(m => m.ImdbId == "tt1375666" && m.MediaType == (int)MediaType.Movie);
        response.Items.Should().Contain(m => m.ImdbId == "tt2085059" && m.MediaType == (int)MediaType.Series);

        var inception = response.Items.Single(m => m.ImdbId == "tt1375666");
        inception.TmdbId.Should().Be(27205);
        inception.RuntimeMinutes.Should().Be(148);
        inception.Director.Should().Be("Christopher Nolan");
        inception.PlatformIds.Should().NotBeEmpty();
        inception.MoodIds.Should().NotBeEmpty();
        inception.GenreIds.Should().NotBeEmpty();
        inception.PlatformLinks.Should().Contain(pl => pl.PlatformSlug == "netflix" && !string.IsNullOrWhiteSpace(pl.AppSchemeUrl));
        inception.Translations.Should().ContainKeys("tr", "en");
        inception.Translations["tr"].Title.Should().Be("Başlangıç");
        inception.Translations["en"].Title.Should().Be("Inception");
    }

    [Fact]
    public async Task GetBulkMediaForShuffle_Should_FilterByMediaType_And_ClampPagination()
    {
        var client = CreateGrpcClient(out var channel);
        using var _ = channel;

        var moviesOnly = await client.GetBulkMediaForShuffleAsync(new GetBulkMediaForShuffleRequest
        {
            PageNumber = -10,
            PageSize = 9999,
            MediaType = (int)MediaType.Movie
        });

        moviesOnly.PageNumber.Should().Be(1);
        moviesOnly.PageSize.Should().Be(100);
        moviesOnly.Items.Should().NotBeEmpty();
        moviesOnly.Items.Should().OnlyContain(m => m.MediaType == (int)MediaType.Movie);

        var seriesOnly = await client.GetBulkMediaForShuffleAsync(new GetBulkMediaForShuffleRequest
        {
            PageNumber = 1,
            PageSize = 10,
            MediaType = (int)MediaType.Series
        });

        seriesOnly.Items.Should().NotBeEmpty();
        seriesOnly.Items.Should().OnlyContain(m => m.MediaType == (int)MediaType.Series);
    }

    [Fact]
    public async Task GetBulkMediaForShuffle_Should_ThrowInvalidArgument_When_MediaTypeIsInvalid()
    {
        var client = CreateGrpcClient(out var channel);
        using var _ = channel;

        var action = async () => await client.GetBulkMediaForShuffleAsync(new GetBulkMediaForShuffleRequest
        {
            PageNumber = 1,
            PageSize = 10,
            MediaType = 99
        });

        var exception = await action.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GetStandaloneEpisodesForRoulette_Should_PaginateAllStandaloneEpisodes_And_ClampPageSize()
    {
        var client = CreateGrpcClient(out var channel);
        using var _ = channel;

        var page1 = await client.GetStandaloneEpisodesForRouletteAsync(new GetStandaloneEpisodesForRouletteRequest
        {
            PageNumber = 1,
            PageSize = 2
        });

        page1.TotalCount.Should().BeGreaterThanOrEqualTo(4);
        page1.PageNumber.Should().Be(1);
        page1.PageSize.Should().Be(2);
        page1.Items.Should().HaveCount(2);

        var page2 = await client.GetStandaloneEpisodesForRouletteAsync(new GetStandaloneEpisodesForRouletteRequest
        {
            PageNumber = 2,
            PageSize = 2
        });

        page2.PageNumber.Should().Be(2);
        page2.PageSize.Should().Be(2);
        page2.Items.Should().HaveCount(2);

        var allFetchedIds = page1.Items.Concat(page2.Items).Select(e => e.EpisodeId).ToList();
        allFetchedIds.Should().OnlyHaveUniqueItems();

        var clamped = await client.GetStandaloneEpisodesForRouletteAsync(new GetStandaloneEpisodesForRouletteRequest
        {
            PageNumber = -1,
            PageSize = 500
        });

        clamped.PageNumber.Should().Be(1);
        clamped.PageSize.Should().Be(100);
        clamped.TotalCount.Should().Be(page1.TotalCount);

        var sanJunipero = clamped.Items.FirstOrDefault(e =>
            e.Translations.TryGetValue("en", out var tr) && tr.Title == "San Junipero");

        sanJunipero.Should().NotBeNull();
        sanJunipero!.IsStandalone.Should().BeTrue();
        sanJunipero.SeasonNumber.Should().Be(3);
        sanJunipero.EpisodeNumber.Should().Be(4);
        sanJunipero.RuntimeMinutes.Should().Be(61);
        sanJunipero.MoodIds.Should().NotBeEmpty();
        sanJunipero.GenreIds.Should().NotBeEmpty();
        sanJunipero.PlatformLinks.Should().Contain(pl => pl.PlatformSlug == "netflix");
        sanJunipero.Translations.Should().ContainKeys("tr", "en");
        sanJunipero.SeriesTranslations.Should().ContainKeys("tr", "en");
        sanJunipero.SeriesTranslations["en"].Title.Should().Be("Black Mirror");
    }

    [Fact]
    public async Task GetStandaloneEpisodesForRoulette_Should_FallbackToSeriesPlatforms_When_EpisodeHasNoOwnPlatform()
    {
        Guid createdEpisodeId = Guid.Empty;
        await ExecuteDbContextAsync(async db =>
        {
            var blackMirror = await db.Series
                .Include(s => s.Seasons)
                .ThenInclude(sn => sn.Episodes)
                .FirstAsync(s => s.ImdbId == "tt2085059");

            var ep = blackMirror.AddEpisode(4, 1, isStandalone: true, runtimeMinutes: 76, imdbRating: 8.3);
            ep.AddOrUpdateTranslation("en", "USS Callister", "Space fleet simulation");
            ep.ClearDomainEvents();
            blackMirror.ClearDomainEvents();
            await db.SaveChangesAsync();
            createdEpisodeId = ep.Id;
        });

        var client = CreateGrpcClient(out var channel);
        using var _ = channel;

        var response = await client.GetStandaloneEpisodesForRouletteAsync(new GetStandaloneEpisodesForRouletteRequest
        {
            PageNumber = 1,
            PageSize = 50
        });

        var callister = response.Items.SingleOrDefault(e => e.EpisodeId == createdEpisodeId.ToString());
        callister.Should().NotBeNull();
        callister!.PlatformLinks.Should().Contain(pl => pl.PlatformSlug == "netflix");
    }

    [Fact]
    public async Task GetStandaloneEpisodesForRoulette_Should_FallbackToSeriesPlatforms_When_EpisodePlatformIsSoftDeleted()
    {
        Guid createdEpisodeId = Guid.Empty;
        Guid deletedPlatformId = Guid.Empty;
        await ExecuteDbContextAsync(async db =>
        {
            var blackMirror = await db.Series
                .Include(s => s.Seasons)
                .ThenInclude(sn => sn.Episodes)
                .FirstAsync(s => s.ImdbId == "tt2085059");

            var prime = await db.Platforms.FirstAsync(p => p.Slug == "prime-video");
            deletedPlatformId = prime.Id;

            var ep = blackMirror.AddEpisode(4, 2, isStandalone: true, runtimeMinutes: 52, imdbRating: 7.4);
            ep.AddOrUpdateTranslation("en", "Arkangel", "Parental control implant");
            ep.AddPlatform(prime.Id, "https://www.primevideo.com/detail/arkangel");
            ep.ClearDomainEvents();
            blackMirror.ClearDomainEvents();
            await db.SaveChangesAsync();
            createdEpisodeId = ep.Id;

            prime.SoftDelete("integration-test");
            prime.ClearDomainEvents();
            await db.SaveChangesAsync();
        });

        var client = CreateGrpcClient(out var channel);
        using var _ = channel;

        var response = await client.GetStandaloneEpisodesForRouletteAsync(new GetStandaloneEpisodesForRouletteRequest
        {
            PageNumber = 1,
            PageSize = 50
        });

        var arkangel = response.Items.SingleOrDefault(e => e.EpisodeId == createdEpisodeId.ToString());
        arkangel.Should().NotBeNull();
        arkangel!.PlatformIds.Should().NotContain(deletedPlatformId.ToString());
        arkangel.PlatformLinks.Should().NotContain(pl => pl.PlatformSlug == "prime-video");
        arkangel.PlatformLinks.Should().Contain(pl => pl.PlatformSlug == "netflix");
    }

    [Fact]
    public async Task GetStandaloneEpisodesForRoulette_Should_ExcludeEpisodes_When_ParentSeriesIsSoftDeleted()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var blackMirror = await db.Series
                .Include(s => s.Seasons)
                .ThenInclude(sn => sn.Episodes)
                .FirstAsync(s => s.ImdbId == "tt2085059");

            blackMirror.Delete("integration-test");
            await db.SaveChangesAsync();
        });

        var client = CreateGrpcClient(out var channel);
        using var _ = channel;

        var response = await client.GetStandaloneEpisodesForRouletteAsync(new GetStandaloneEpisodesForRouletteRequest
        {
            PageNumber = 1,
            PageSize = 50
        });

        response.Items.Should().NotContain(e =>
            e.SeriesTranslations.ContainsKey("en") && e.SeriesTranslations["en"].Title == "Black Mirror");
    }

    [Fact]
    public async Task GetMediaById_Should_ExcludeSoftDeletedPlatformIdsAndLinks()
    {
        Guid inceptionId = Guid.Empty;
        Guid primeId = Guid.Empty;
        await ExecuteDbContextAsync(async db =>
        {
            inceptionId = await db.Movies
                .Where(m => m.ImdbId == "tt1375666")
                .Select(m => m.Id)
                .FirstAsync();

            var prime = await db.Platforms.FirstAsync(p => p.Slug == "prime-video");
            primeId = prime.Id;
            prime.SoftDelete("integration-test");
            prime.ClearDomainEvents();
            await db.SaveChangesAsync();
        });

        var client = CreateGrpcClient(out var channel);
        using var _ = channel;

        var result = await client.GetMediaByIdAsync(new GetMediaByIdRequest
        {
            MediaId = inceptionId.ToString()
        });

        result.PlatformIds.Should().NotContain(primeId.ToString());
        result.PlatformLinks.Should().NotContain(pl => pl.PlatformSlug == "prime-video");
        result.PlatformLinks.Should().Contain(pl => pl.PlatformSlug == "netflix");
    }

    [Fact]
    public async Task GetMediaById_Should_ReturnMediaModel_When_MediaExists()
    {
        var inceptionId = await ExecuteDbContextAsync(async db =>
            await db.Movies
                .Where(m => m.ImdbId == "tt1375666")
                .Select(m => m.Id)
                .FirstAsync());

        var client = CreateGrpcClient(out var channel);
        using var _ = channel;

        var result = await client.GetMediaByIdAsync(new GetMediaByIdRequest
        {
            MediaId = inceptionId.ToString()
        });

        result.Id.Should().Be(inceptionId.ToString());
        result.ImdbId.Should().Be("tt1375666");
        result.Translations["tr"].Title.Should().Be("Başlangıç");
        result.Translations["en"].Title.Should().Be("Inception");
    }

    [Fact]
    public async Task GetMediaById_Should_ThrowNotFound_When_MediaDoesNotExist()
    {
        var client = CreateGrpcClient(out var channel);
        using var _ = channel;

        var action = async () => await client.GetMediaByIdAsync(new GetMediaByIdRequest
        {
            MediaId = Guid.NewGuid().ToString()
        });

        var exception = await action.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task GetMediaById_Should_ThrowInvalidArgument_When_MediaIdIsInvalid(string invalidMediaId)
    {
        var client = CreateGrpcClient(out var channel);
        using var _ = channel;

        var action = async () => await client.GetMediaByIdAsync(new GetMediaByIdRequest
        {
            MediaId = invalidMediaId
        });

        var exception = await action.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }
}
