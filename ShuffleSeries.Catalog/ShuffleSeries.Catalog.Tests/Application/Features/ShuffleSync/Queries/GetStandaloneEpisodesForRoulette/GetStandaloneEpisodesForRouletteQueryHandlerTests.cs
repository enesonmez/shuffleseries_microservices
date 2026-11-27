using Moq;
using ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetStandaloneEpisodesForRoulette;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Requests;
using SeriesEntity = ShuffleSeries.Catalog.Domain.Entities.Series;

namespace ShuffleSeries.Catalog.Tests.Application.Features.ShuffleSync.Queries.GetStandaloneEpisodesForRoulette;

public class GetStandaloneEpisodesForRouletteQueryHandlerTests
{
    private readonly Mock<IEpisodeRepository> _episodeRepositoryMock = new();
    private readonly GetStandaloneEpisodesForRouletteQueryHandler _handler;

    public GetStandaloneEpisodesForRouletteQueryHandlerTests()
    {
        _handler = new GetStandaloneEpisodesForRouletteQueryHandler(_episodeRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_MapStandaloneEpisodes_WithSeriesContextAndFallbackPlatforms_And_ClampPagination()
    {
        var series = SeriesEntity.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg", imdbRating: 8.7);
        series.AddOrUpdateTranslation("tr", "Black Mirror", "Antoloji dizisi");
        series.AddOrUpdateTranslation("en", "Black Mirror", "Anthology series");

        var seriesPlatformId = Guid.NewGuid();
        var moodId = Guid.NewGuid();
        var genreId = Guid.NewGuid();
        series.AddPlatform(seriesPlatformId, "https://netflix.com/title/70264888", "netflix://title/70264888");
        series.AddMood(moodId);
        series.AddGenre(genreId);

        var episodeWithOwnPlatform = series.AddEpisode(3, 4, isStandalone: true, runtimeMinutes: 61, imdbRating: 8.6, thumbnailUrl: "https://example.com/s3e4.jpg");
        episodeWithOwnPlatform.AddOrUpdateTranslation("tr", "San Junipero", "1987 sahil kasabası");
        var epPlatformId = Guid.NewGuid();
        episodeWithOwnPlatform.AddPlatform(epPlatformId, "https://netflix.com/watch/80104628", "netflix://watch/80104628");

        var episodeWithoutOwnPlatform = series.AddEpisode(1, 1, isStandalone: true, runtimeMinutes: 44, imdbRating: 7.7);
        episodeWithoutOwnPlatform.AddOrUpdateTranslation("en", "The National Anthem", "Prime minister dilemma");

        var s3 = series.Seasons.First(s => s.SeasonNumber == 3);
        var s1 = series.Seasons.First(s => s.SeasonNumber == 1);

        _episodeRepositoryMock
            .Setup(x => x.GetStandaloneEpisodesWithSeriesContextAsync(1, PaginationRequest.MaxPageSize, series.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[]
            {
                new StandaloneEpisodeContext(episodeWithOwnPlatform, s3, series),
                new StandaloneEpisodeContext(episodeWithoutOwnPlatform, s1, series)
            }, 250));

        var result = await _handler.Handle(new GetStandaloneEpisodesForRouletteQuery(pageNumber: -5, pageSize: 5000, seriesId: series.Id), CancellationToken.None);

        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(PaginationRequest.MaxPageSize);
        result.TotalCount.Should().Be(250);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.Items.Should().HaveCount(2);

        var first = result.Items.ElementAt(0);
        first.SeriesId.Should().Be(series.Id);
        first.SeasonNumber.Should().Be(3);
        first.EpisodeNumber.Should().Be(4);
        first.IsStandalone.Should().BeTrue();
        first.SeriesPosterUrl.Should().Be("https://example.com/bm.jpg");
        first.ReleaseYear.Should().Be(2011);
        first.MoodIds.Should().ContainSingle().Which.Should().Be(moodId);
        first.GenreIds.Should().ContainSingle().Which.Should().Be(genreId);
        first.PlatformIds.Should().ContainSingle().Which.Should().Be(epPlatformId);
        first.Translations["tr"].Title.Should().Be("San Junipero");
        first.SeriesTranslations.Should().ContainKeys("tr", "en");

        var second = result.Items.ElementAt(1);
        second.PlatformIds.Should().ContainSingle().Which.Should().Be(seriesPlatformId);
        second.Translations["en"].Title.Should().Be("The National Anthem");
    }

    [Fact]
    public async Task Handle_Should_UsePaginationRequestDefaults_When_ParametersAreNonPositive()
    {
        _episodeRepositoryMock
            .Setup(x => x.GetStandaloneEpisodesWithSeriesContextAsync(
                PaginationRequest.DefaultPageNumber,
                PaginationRequest.DefaultPageSize,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<StandaloneEpisodeContext>(), 0));

        var result = await _handler.Handle(new GetStandaloneEpisodesForRouletteQuery(pageNumber: 0, pageSize: 0), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.PageNumber.Should().Be(PaginationRequest.DefaultPageNumber);
        result.PageSize.Should().Be(PaginationRequest.DefaultPageSize);
        result.TotalCount.Should().Be(0);
        _episodeRepositoryMock.Verify(
            x => x.GetStandaloneEpisodesWithSeriesContextAsync(
                PaginationRequest.DefaultPageNumber,
                PaginationRequest.DefaultPageSize,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
