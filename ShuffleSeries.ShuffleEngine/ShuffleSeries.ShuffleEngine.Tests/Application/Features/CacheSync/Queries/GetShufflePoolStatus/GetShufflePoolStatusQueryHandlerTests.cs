using Moq;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Common.Models;
using ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Queries.GetShufflePoolStatus;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Features.CacheSync.Queries.GetShufflePoolStatus;

public class GetShufflePoolStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMappedPoolStatistics()
    {
        var now = DateTime.UtcNow;
        var storeMock = new Mock<IShufflePoolStore>();
        storeMock
            .Setup(x => x.GetPoolStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShufflePoolStatistics(true, 12, 8, 25, 9, 6, 19, now));

        var handler = new GetShufflePoolStatusQueryHandler(storeMock.Object);
        var response = await handler.Handle(new GetShufflePoolStatusQuery(), CancellationToken.None);

        response.IsWarmed.Should().BeTrue();
        response.MoviesCount.Should().Be(12);
        response.SeriesCount.Should().Be(8);
        response.StandaloneEpisodesCount.Should().Be(25);
        response.SafetyNetMoviesCount.Should().Be(9);
        response.SafetyNetSeriesCount.Should().Be(6);
        response.SafetyNetEpisodesCount.Should().Be(19);
        response.LastWarmedAtUtc.Should().Be(now);
    }
}
