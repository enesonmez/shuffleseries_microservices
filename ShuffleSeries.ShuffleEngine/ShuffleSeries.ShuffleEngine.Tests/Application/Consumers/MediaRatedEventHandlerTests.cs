using Moq;
using ShuffleSeries.Shared.Core.Domain.Enums;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Common.Models;
using ShuffleSeries.ShuffleEngine.Application.Consumers;
using ShuffleSeries.ShuffleEngine.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Domain.Models;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Consumers;

public class MediaRatedEventHandlerTests
{
    [Fact]
    public async Task Handle_WhenMediaContextInPool_ShouldApplyAffinityDirectly()
    {
        var userId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var moodId = Guid.NewGuid();
        var genreId = Guid.NewGuid();
        var evt = new MediaRatedEvent(Guid.NewGuid(), userId, mediaId, MediaRatingTier.DoubleThumbsUp, DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<MediaRatedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var affinityCtx = new ShuffleMediaAffinityContext(mediaId, [moodId], [genreId], "Christopher Nolan");
        var poolMock = new Mock<IShufflePoolStore>();
        poolMock.Setup(x => x.GetMediaAffinityContextAsync(mediaId, It.IsAny<CancellationToken>())).ReturnsAsync(affinityCtx);

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        var stateMock = new Mock<IUserShuffleStateStore>();

        var handler = new MediaRatedEventHandler(inboxMock.Object, poolMock.Object, catalogMock.Object, stateMock.Object);
        await handler.Handle(evt, CancellationToken.None);

        catalogMock.Verify(x => x.GetMediaByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        stateMock.Verify(x => x.ApplyMediaRatingAffinityAsync(userId, affinityCtx, MediaRatingTier.DoubleThumbsUp, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPoolMiss_ShouldFallbackToGrpcAndUpdateAffinity()
    {
        var userId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var moodId = Guid.NewGuid();
        var evt = new MediaRatedEvent(Guid.NewGuid(), userId, mediaId, MediaRatingTier.Disliked, DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<MediaRatedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var poolMock = new Mock<IShufflePoolStore>();
        poolMock.Setup(x => x.GetMediaAffinityContextAsync(mediaId, It.IsAny<CancellationToken>())).ReturnsAsync((ShuffleMediaAffinityContext?)null);

        var fetched = new ShuffleMediaSnapshot(
            mediaId, ShuffleMediaKind.Movie, 100, "tt0100000", 6.2, 5000, 2020,
            "https://img/m.jpg", null, 100, "Jane Doe", null, null, 6.0, false,
            [], [moodId], [], [], new Dictionary<string, ShuffleLocalizedText>());

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        catalogMock.Setup(x => x.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>())).ReturnsAsync(fetched);

        var stateMock = new Mock<IUserShuffleStateStore>();

        var handler = new MediaRatedEventHandler(inboxMock.Object, poolMock.Object, catalogMock.Object, stateMock.Object);
        await handler.Handle(evt, CancellationToken.None);

        poolMock.Verify(x => x.UpsertMediaBatchAsync(It.IsAny<IReadOnlyList<ShuffleMediaSnapshot>>(), evt.OccurredOnUtc.Ticks, It.IsAny<CancellationToken>()), Times.Once);
        stateMock.Verify(x => x.ApplyMediaRatingAffinityAsync(userId, It.Is<ShuffleMediaAffinityContext>(c => c.MediaId == mediaId && c.Director == "Jane Doe"), MediaRatingTier.Disliked, It.IsAny<CancellationToken>()), Times.Once);
    }
}
