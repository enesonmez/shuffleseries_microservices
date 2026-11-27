using Moq;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Consumers;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Consumers;

public class CatalogTaxonomyChangedEventHandlerTests
{
    [Fact]
    public async Task Handle_WhenTaxonomyDeleted_ShouldRemoveTaxonomyIndex()
    {
        var moodId = Guid.NewGuid();
        var evt = new CatalogTaxonomyChangedEvent(Guid.NewGuid(), "Mood", moodId, "Deleted", DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<CatalogTaxonomyChangedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var poolMock = new Mock<IShufflePoolStore>();
        var warmMock = new Mock<IShuffleCacheWarmingService>();
        var handler = new CatalogTaxonomyChangedEventHandler(inboxMock.Object, poolMock.Object, warmMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        poolMock.Verify(x => x.RemoveTaxonomyIndexAsync("Mood", moodId, It.IsAny<CancellationToken>()), Times.Once);
        warmMock.Verify(x => x.EnsureWarmedAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Platform")]
    [InlineData("Mood")]
    public async Task Handle_WhenPlatformOrMoodUpdated_ShouldTriggerForceRebuild(string taxonomyType)
    {
        var taxonomyId = Guid.NewGuid();
        var evt = new CatalogTaxonomyChangedEvent(Guid.NewGuid(), taxonomyType, taxonomyId, "Updated", DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<CatalogTaxonomyChangedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var poolMock = new Mock<IShufflePoolStore>();
        var warmMock = new Mock<IShuffleCacheWarmingService>();
        var handler = new CatalogTaxonomyChangedEventHandler(inboxMock.Object, poolMock.Object, warmMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        warmMock.Verify(x => x.EnsureWarmedAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
