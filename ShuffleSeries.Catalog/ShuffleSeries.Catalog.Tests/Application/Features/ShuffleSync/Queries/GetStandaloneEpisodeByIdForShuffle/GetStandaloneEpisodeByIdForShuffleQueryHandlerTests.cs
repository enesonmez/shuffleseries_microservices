using Moq;
using ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetStandaloneEpisodeByIdForShuffle;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using SeriesEntity = ShuffleSeries.Catalog.Domain.Entities.Series;

namespace ShuffleSeries.Catalog.Tests.Application.Features.ShuffleSync.Queries.GetStandaloneEpisodeByIdForShuffle;

public class GetStandaloneEpisodeByIdForShuffleQueryHandlerTests
{
    private readonly Mock<IEpisodeRepository> _episodeRepositoryMock = new();
    private readonly GetStandaloneEpisodeByIdForShuffleQueryHandler _handler;

    public GetStandaloneEpisodeByIdForShuffleQueryHandlerTests()
    {
        _handler = new GetStandaloneEpisodeByIdForShuffleQueryHandler(_episodeRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEpisodeExists_ShouldMapStandaloneResponse()
    {
        var series = SeriesEntity.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg", imdbRating: 8.7);
        series.AddOrUpdateTranslation("en", "Black Mirror", "Anthology");
        var episode = series.AddEpisode(3, 4, isStandalone: true, runtimeMinutes: 61, imdbRating: 8.6);
        episode.AddOrUpdateTranslation("en", "San Junipero", "Overview");
        var season = series.Seasons.First();

        _episodeRepositoryMock
            .Setup(x => x.GetStandaloneEpisodeByIdWithSeriesContextAsync(episode.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StandaloneEpisodeContext(episode, season, series));

        var result = await _handler.Handle(new GetStandaloneEpisodeByIdForShuffleQuery(episode.Id), CancellationToken.None);

        result.EpisodeId.Should().Be(episode.Id);
        result.SeriesId.Should().Be(series.Id);
        result.Translations["en"].Title.Should().Be("San Junipero");
    }

    [Fact]
    public async Task Handle_WhenEpisodeDoesNotExist_ShouldThrowEpisodeNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _episodeRepositoryMock
            .Setup(x => x.GetStandaloneEpisodeByIdWithSeriesContextAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StandaloneEpisodeContext?)null);

        var act = async () => await _handler.Handle(new GetStandaloneEpisodeByIdForShuffleQuery(missingId), CancellationToken.None);

        await act.Should().ThrowAsync<EpisodeNotFoundException>();
    }
}
