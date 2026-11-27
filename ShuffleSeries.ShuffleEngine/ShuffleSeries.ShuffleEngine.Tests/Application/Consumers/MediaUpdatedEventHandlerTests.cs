using Moq;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Consumers;
using ShuffleSeries.ShuffleEngine.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Domain.Models;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Consumers;

public class MediaUpdatedEventHandlerTests
{
    [Fact]
    public async Task Handle_WhenMediaWasDeletedBeforeSync_ShouldRemoveFromPool()
    {
        var mediaId = Guid.NewGuid();
        var evt = new MediaUpdatedEvent(Guid.NewGuid(), mediaId, "Movie", DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<MediaUpdatedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        catalogMock.Setup(x => x.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>())).ReturnsAsync((ShuffleMediaSnapshot?)null);

        var poolMock = new Mock<IShufflePoolStore>();
        var handler = new MediaUpdatedEventHandler(inboxMock.Object, catalogMock.Object, poolMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        poolMock.Verify(x => x.RemoveMediaAsync(mediaId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMovieExists_ShouldUpsertUpdatedSnapshot()
    {
        var mediaId = Guid.NewGuid();
        var evt = new MediaUpdatedEvent(Guid.NewGuid(), mediaId, "Movie", DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<MediaUpdatedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var movie = new ShuffleMediaSnapshot(
            mediaId, ShuffleMediaKind.Movie, 27205, "tt1375666", 8.8, 2500000, 2010,
            "https://img/inc.jpg", null, 148, "Christopher Nolan", null, null, 8.9, true,
            [], [], [], [], new Dictionary<string, ShuffleLocalizedText>());

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        catalogMock.Setup(x => x.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>())).ReturnsAsync(movie);

        var poolMock = new Mock<IShufflePoolStore>();
        var handler = new MediaUpdatedEventHandler(inboxMock.Object, catalogMock.Object, poolMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        poolMock.Verify(x => x.UpsertMediaBatchAsync(It.Is<IReadOnlyList<ShuffleMediaSnapshot>>(l => l.Count == 1 && l[0].Id == mediaId), evt.OccurredOnUtc.Ticks, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSeriesHasNoRemainingStandaloneEpisodes_ShouldStillReconcileAndEvictOrphans()
    {
        var seriesId = Guid.NewGuid();
        var evt = new MediaUpdatedEvent(Guid.NewGuid(), seriesId, "Series", DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<MediaUpdatedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var series = new ShuffleMediaSnapshot(
            seriesId, ShuffleMediaKind.Series, 42009, "tt2085059", 8.7, 620000, 2011,
            "https://img/bm.jpg", null, null, null, 2, 6, 8.5, true,
            [], [], [], [], new Dictionary<string, ShuffleLocalizedText>());

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        catalogMock.Setup(x => x.GetMediaByIdAsync(seriesId, It.IsAny<CancellationToken>())).ReturnsAsync(series);
        catalogMock.Setup(x => x.GetStandaloneEpisodesBySeriesIdAsync(seriesId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var poolMock = new Mock<IShufflePoolStore>();
        var handler = new MediaUpdatedEventHandler(inboxMock.Object, catalogMock.Object, poolMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        poolMock.Verify(
            x => x.ReconcileSeriesEpisodesAsync(
                seriesId,
                It.Is<IReadOnlyList<ShuffleEpisodeSnapshot>>(l => l.Count == 0),
                evt.OccurredOnUtc.Ticks,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
