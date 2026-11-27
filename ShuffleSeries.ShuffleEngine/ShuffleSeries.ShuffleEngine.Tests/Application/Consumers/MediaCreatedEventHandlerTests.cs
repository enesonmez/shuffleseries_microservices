using Moq;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Consumers;
using ShuffleSeries.ShuffleEngine.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Domain.Models;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Consumers;

public class MediaCreatedEventHandlerTests
{
    [Fact]
    public async Task Handle_WhenSeriesCreated_ShouldUpsertMediaAndStandaloneEpisodes()
    {
        var mediaId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        var evt = new MediaCreatedEvent(Guid.NewGuid(), mediaId, "Series", DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<MediaCreatedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var seriesSnapshot = new ShuffleMediaSnapshot(
            mediaId, ShuffleMediaKind.Series, 42009, "tt2085059", 8.7, 600000, 2011,
            "https://img/bm.jpg", null, null, null, 2, 6, 8.6, true,
            [], [], [], [], new Dictionary<string, ShuffleLocalizedText>());

        var episodeSnapshot = new ShuffleEpisodeSnapshot(
            episodeId, mediaId, Guid.NewGuid(), 1, 1, 44, 8.2, null, "https://img/bm.jpg", 2011,
            8.1, true, [], [], [], [],
            new Dictionary<string, ShuffleLocalizedText>(),
            new Dictionary<string, ShuffleLocalizedText>());

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        catalogMock.Setup(x => x.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>())).ReturnsAsync(seriesSnapshot);
        catalogMock.Setup(x => x.GetStandaloneEpisodesBySeriesIdAsync(mediaId, It.IsAny<CancellationToken>())).ReturnsAsync([episodeSnapshot]);

        var poolMock = new Mock<IShufflePoolStore>();
        var handler = new MediaCreatedEventHandler(inboxMock.Object, catalogMock.Object, poolMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        poolMock.Verify(x => x.UpsertMediaBatchAsync(It.Is<IReadOnlyList<ShuffleMediaSnapshot>>(l => l.Count == 1 && l[0].Id == mediaId), evt.OccurredOnUtc.Ticks, It.IsAny<CancellationToken>()), Times.Once);
        poolMock.Verify(x => x.UpsertEpisodesBatchAsync(It.Is<IReadOnlyList<ShuffleEpisodeSnapshot>>(l => l.Count == 1 && l[0].EpisodeId == episodeId), evt.OccurredOnUtc.Ticks, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMediaNotFoundInCatalog_ShouldSkipUpsert()
    {
        var mediaId = Guid.NewGuid();
        var evt = new MediaCreatedEvent(Guid.NewGuid(), mediaId, "Movie", DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<MediaCreatedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        catalogMock.Setup(x => x.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>())).ReturnsAsync((ShuffleMediaSnapshot?)null);

        var poolMock = new Mock<IShufflePoolStore>();
        var handler = new MediaCreatedEventHandler(inboxMock.Object, catalogMock.Object, poolMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        poolMock.Verify(x => x.UpsertMediaBatchAsync(It.IsAny<IReadOnlyList<ShuffleMediaSnapshot>>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
