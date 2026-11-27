using Moq;
using ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetBulkMediaForShuffle;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Requests;
using SeriesEntity = ShuffleSeries.Catalog.Domain.Entities.Series;

namespace ShuffleSeries.Catalog.Tests.Application.Features.ShuffleSync.Queries.GetBulkMediaForShuffle;

public class GetBulkMediaForShuffleQueryHandlerTests
{
    private readonly Mock<IMediaRepository> _mediaRepositoryMock = new();
    private readonly GetBulkMediaForShuffleQueryHandler _handler;

    public GetBulkMediaForShuffleQueryHandlerTests()
    {
        _handler = new GetBulkMediaForShuffleQueryHandler(_mediaRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_NormalizePaginationBounds_And_MapMovieAndSeriesTranslations()
    {
        var movie = Movie.Create(27205, "tt1375666", 2010, 148, "https://example.com/inception.jpg", imdbRating: 8.8, imdbVotes: 2500000, director: "Christopher Nolan");
        movie.AddOrUpdateTranslation("tr", "Başlangıç", "Rüya içinde rüya", "Rüyanız gerçek dünyanızdır");
        movie.AddOrUpdateTranslation("en", "Inception", "Dream sharing thief", "Your mind is the scene of the crime");
        var platformId = Guid.NewGuid();
        var moodId = Guid.NewGuid();
        var genreId = Guid.NewGuid();
        movie.AddPlatform(platformId, "https://netflix.com/watch/1", "netflix://watch/1");
        movie.AddMood(moodId);
        movie.AddGenre(genreId);

        var series = SeriesEntity.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/bm.jpg", imdbRating: 8.7, totalSeasons: 6);
        series.AddOrUpdateTranslation("en", "Black Mirror", "Anthology series");

        _mediaRepositoryMock
            .Setup(x => x.GetBulkForShuffleAsync(null, 1, PaginationRequest.MaxPageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new Media[] { movie, series }, 2));

        var query = new GetBulkMediaForShuffleQuery(pageNumber: -5, pageSize: 999999, mediaType: null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(PaginationRequest.MaxPageSize);
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);

        var movieDto = result.Items.ElementAt(0);
        movieDto.MediaType.Should().Be(MediaType.Movie);
        movieDto.RuntimeMinutes.Should().Be(148);
        movieDto.Director.Should().Be("Christopher Nolan");
        movieDto.PlatformIds.Should().ContainSingle().Which.Should().Be(platformId);
        movieDto.MoodIds.Should().ContainSingle().Which.Should().Be(moodId);
        movieDto.GenreIds.Should().ContainSingle().Which.Should().Be(genreId);
        movieDto.Translations.Should().ContainKeys("tr", "en");
        movieDto.Translations["tr"].Title.Should().Be("Başlangıç");
        movieDto.Translations["en"].Title.Should().Be("Inception");

        var seriesDto = result.Items.ElementAt(1);
        seriesDto.MediaType.Should().Be(MediaType.Series);
        seriesDto.SeriesType.Should().Be(SeriesType.EpisodicAnthology);
        seriesDto.TotalSeasons.Should().Be(6);
        seriesDto.Translations.Should().ContainKey("en");
    }

    [Fact]
    public async Task Handle_Should_PassMediaTypeFilter_When_Specified()
    {
        _mediaRepositoryMock
            .Setup(x => x.GetBulkForShuffleAsync(MediaType.Movie, 2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Media>(), 0));

        var result = await _handler.Handle(new GetBulkMediaForShuffleQuery(2, 25, MediaType.Movie), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(25);
        _mediaRepositoryMock.Verify(x => x.GetBulkForShuffleAsync(MediaType.Movie, 2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_UsePaginationRequestDefaults_When_ParametersAreNonPositive()
    {
        _mediaRepositoryMock
            .Setup(x => x.GetBulkForShuffleAsync(null, PaginationRequest.DefaultPageNumber, PaginationRequest.DefaultPageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Media>(), 0));

        var result = await _handler.Handle(new GetBulkMediaForShuffleQuery(0, 0, null), CancellationToken.None);

        result.PageNumber.Should().Be(PaginationRequest.DefaultPageNumber);
        result.PageSize.Should().Be(PaginationRequest.DefaultPageSize);
        result.Items.Should().BeEmpty();
    }
}
