using Moq;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Consumers;
using ShuffleSeries.ShuffleEngine.Domain.Models;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Consumers;

public class EpisodeUpdatedEventHandlerTests
{
    [Fact]
    public async Task Handle_WhenChangedToNonStandalone_ShouldRemoveEpisodeFromPool()
    {
        var epId = Guid.NewGuid();
        var evt = new EpisodeUpdatedEvent(Guid.NewGuid(), epId, 2, IsStandalone: false, 45, 7.8, DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<EpisodeUpdatedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        var poolMock = new Mock<IShufflePoolStore>();
        var handler = new EpisodeUpdatedEventHandler(inboxMock.Object, catalogMock.Object, poolMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        poolMock.Verify(x => x.RemoveEpisodeAsync(epId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStandaloneAndFound_ShouldUpsertEpisode()
    {
        var epId = Guid.NewGuid();
        var evt = new EpisodeUpdatedEvent(Guid.NewGuid(), epId, 4, IsStandalone: true, 61, 8.6, DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<EpisodeUpdatedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var epSnapshot = new ShuffleEpisodeSnapshot(
            epId, Guid.NewGuid(), Guid.NewGuid(), 3, 4, 61, 8.6, null, "https://img/bm.jpg", 2016,
            8.4, true, [], [], [], [],
            new Dictionary<string, ShuffleLocalizedText>(),
            new Dictionary<string, ShuffleLocalizedText>());

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        catalogMock.Setup(x => x.GetStandaloneEpisodeByIdAsync(epId, It.IsAny<CancellationToken>())).ReturnsAsync(epSnapshot);

        var poolMock = new Mock<IShufflePoolStore>();
        var handler = new EpisodeUpdatedEventHandler(inboxMock.Object, catalogMock.Object, poolMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        poolMock.Verify(x => x.UpsertEpisodesBatchAsync(It.Is<IReadOnlyList<ShuffleEpisodeSnapshot>>(l => l.Count == 1 && l[0].EpisodeId == epId), evt.OccurredOnUtc.Ticks, It.IsAny<CancellationToken>()), Times.Once);
    }
}
