using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ShuffleSeries.ShuffleEngine.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Domain.Exceptions;
using ShuffleSeries.ShuffleEngine.Infrastructure.Grpc.Catalog;
using ShuffleSeries.ShuffleEngine.Infrastructure.Services;

namespace ShuffleSeries.ShuffleEngine.Tests.Infrastructure.Services;

public class CatalogSyncGrpcClientTests
{
    private static AsyncUnaryCall<T> CreateAsyncUnaryCall<T>(T response) =>
        new(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { });

    private static AsyncUnaryCall<T> CreateFailedUnaryCall<T>(StatusCode statusCode, string detail) =>
        new(
            Task.FromException<T>(new RpcException(new Status(statusCode, detail))),
            Task.FromResult(new Metadata()),
            () => new Status(statusCode, detail),
            () => [],
            () => { });

    [Fact]
    public async Task GetMediaByIdAsync_WhenFound_ShouldMapProtobufToDomainSnapshotWithSafetyNetAndScore()
    {
        var mediaId = Guid.NewGuid();
        var platformId = Guid.NewGuid();
        var moodId = Guid.NewGuid();
        var genreId = Guid.NewGuid();

        var grpcModel = new MediaGrpcModel
        {
            Id = mediaId.ToString(),
            MediaType = 1,
            TmdbId = 27205,
            ImdbId = "tt1375666",
            ImdbRating = 8.8,
            ImdbVotes = 2_500_000,
            ReleaseYear = 2010,
            PosterUrl = "https://img/inc.jpg",
            RuntimeMinutes = 148,
            Director = "Christopher Nolan"
        };
        grpcModel.PlatformIds.Add(platformId.ToString());
        grpcModel.MoodIds.Add(moodId.ToString());
        grpcModel.GenreIds.Add(genreId.ToString());
        grpcModel.Translations["en"] = new LocalizedMediaPayload { Title = "Inception", Overview = "Dream heist" };

        var grpcMock = new Mock<CatalogGrpc.CatalogGrpcClient>();
        grpcMock
            .Setup(c => c.GetMediaByIdAsync(
                It.Is<GetMediaByIdRequest>(r => r.MediaId == mediaId.ToString()),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(grpcModel));

        var client = new CatalogSyncGrpcClient(grpcMock.Object, TimeProvider.System, NullLogger<CatalogSyncGrpcClient>.Instance);

        var snapshot = await client.GetMediaByIdAsync(mediaId);

        snapshot.Should().NotBeNull();
        snapshot!.Id.Should().Be(mediaId);
        snapshot.Kind.Should().Be(ShuffleMediaKind.Movie);
        snapshot.IsSafetyNetEligible.Should().BeTrue();
        snapshot.BaseQualityScore.Should().BeGreaterThan(7.0);
        snapshot.PlatformIds.Should().ContainSingle().Which.Should().Be(platformId);
        snapshot.Translations["en"].Title.Should().Be("Inception");
    }

    [Fact]
    public async Task GetMediaByIdAsync_WhenGrpcNotFound_ShouldReturnNull()
    {
        var mediaId = Guid.NewGuid();
        var grpcMock = new Mock<CatalogGrpc.CatalogGrpcClient>();
        grpcMock
            .Setup(c => c.GetMediaByIdAsync(It.IsAny<GetMediaByIdRequest>(), null, null, It.IsAny<CancellationToken>()))
            .Returns(CreateFailedUnaryCall<MediaGrpcModel>(StatusCode.NotFound, "Not found"));

        var client = new CatalogSyncGrpcClient(grpcMock.Object, TimeProvider.System, NullLogger<CatalogSyncGrpcClient>.Instance);

        var snapshot = await client.GetMediaByIdAsync(mediaId);

        snapshot.Should().BeNull();
    }

    [Fact]
    public async Task GetMediaByIdAsync_WhenGrpcUnavailable_ShouldThrowCatalogSyncFailedException()
    {
        var mediaId = Guid.NewGuid();
        var grpcMock = new Mock<CatalogGrpc.CatalogGrpcClient>();
        grpcMock
            .Setup(c => c.GetMediaByIdAsync(It.IsAny<GetMediaByIdRequest>(), null, null, It.IsAny<CancellationToken>()))
            .Returns(CreateFailedUnaryCall<MediaGrpcModel>(StatusCode.Unavailable, "Catalog down"));

        var client = new CatalogSyncGrpcClient(grpcMock.Object, TimeProvider.System, NullLogger<CatalogSyncGrpcClient>.Instance);

        var act = async () => await client.GetMediaByIdAsync(mediaId);

        await act.Should().ThrowAsync<CatalogSyncFailedException>();
    }

    [Fact]
    public async Task GetBulkMediaPageAsync_ShouldClampPageSizeToMax100()
    {
        var grpcMock = new Mock<CatalogGrpc.CatalogGrpcClient>();
        grpcMock
            .Setup(c => c.GetBulkMediaForShuffleAsync(
                It.Is<GetBulkMediaForShuffleRequest>(r => r.PageNumber == 1 && r.PageSize == 100),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new GetBulkMediaForShuffleResponse { PageNumber = 1, PageSize = 100, TotalCount = 0 }));

        var client = new CatalogSyncGrpcClient(grpcMock.Object, TimeProvider.System, NullLogger<CatalogSyncGrpcClient>.Instance);

        var page = await client.GetBulkMediaPageAsync(pageNumber: 0, pageSize: 5000);

        page.PageNumber.Should().Be(1);
        page.PageSize.Should().Be(100);
        grpcMock.VerifyAll();
    }
}
