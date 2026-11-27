using Moq;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Consumers;
using ShuffleSeries.ShuffleEngine.Domain.Models;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Consumers;

public class EpisodeCreatedEventHandlerTests
{
    [Fact]
    public async Task Handle_WhenNotStandalone_ShouldIgnoreWithoutGrpcCall()
    {
        var evt = new EpisodeCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, IsStandalone: false, DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<EpisodeCreatedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        var poolMock = new Mock<IShufflePoolStore>();
        var handler = new EpisodeCreatedEventHandler(inboxMock.Object, catalogMock.Object, poolMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        catalogMock.Verify(x => x.GetStandaloneEpisodeByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        poolMock.Verify(x => x.UpsertEpisodesBatchAsync(It.IsAny<IReadOnlyList<ShuffleEpisodeSnapshot>>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStandalone_ShouldFetchViaGrpcAndUpsert()
    {
        var epId = Guid.NewGuid();
        var evt = new EpisodeCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), epId, 4, IsStandalone: true, DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<EpisodeCreatedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var epSnapshot = new ShuffleEpisodeSnapshot(
            epId, evt.SeriesId, evt.SeasonId, 3, 4, 61, 8.6, null, "https://img/bm.jpg", 2016,
            8.4, true, [], [], [], [],
            new Dictionary<string, ShuffleLocalizedText>(),
            new Dictionary<string, ShuffleLocalizedText>());

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        catalogMock.Setup(x => x.GetStandaloneEpisodeByIdAsync(epId, It.IsAny<CancellationToken>())).ReturnsAsync(epSnapshot);

        var poolMock = new Mock<IShufflePoolStore>();
        var handler = new EpisodeCreatedEventHandler(inboxMock.Object, catalogMock.Object, poolMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        poolMock.Verify(x => x.UpsertEpisodesBatchAsync(It.Is<IReadOnlyList<ShuffleEpisodeSnapshot>>(l => l.Count == 1 && l[0].EpisodeId == epId), evt.OccurredOnUtc.Ticks, It.IsAny<CancellationToken>()), Times.Once);
    }
}
