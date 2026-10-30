using Moq;
using ShuffleSeries.Catalog.Application.Features.Movies.Queries.GetMovieById;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Movies;

public class GetMovieByIdQueryHandlerTests
{
    private readonly Mock<IMovieRepository> _movieRepositoryMock = new();
    private readonly Mock<ILanguageContext> _languageContextMock = new();
    private readonly GetMovieByIdQueryHandler _handler;

    public GetMovieByIdQueryHandlerTests()
    {
        _languageContextMock.Setup(x => x.CurrentLanguage).Returns("tr");
        _handler = new GetMovieByIdQueryHandler(_movieRepositoryMock.Object, _languageContextMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_MovieDoesNotExist()
    {
        var query = new GetMovieByIdQuery(Guid.NewGuid());

        _movieRepositoryMock.Setup(x => x.GetByIdForDetailAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Movie?)null);

        var action = async () => await _handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<MovieNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnMovieDetailResponse_When_MovieExists()
    {
        var movie = Movie.Create(
            tmdbId: 872585,
            imdbId: "tt15398776",
            releaseYear: 2023,
            runtimeMinutes: 180,
            posterUrl: "https://example.com/oppenheimer.jpg",
            director: "Christopher Nolan",
            imdbRating: 8.9,
            imdbVotes: 700000);
        movie.AddOrUpdateTranslation("tr", "Oppenheimer", "Atom bombasının babası", "Dünyayı sonsuza dek değiştirdi");

        var query = new GetMovieByIdQuery(movie.Id);

        _movieRepositoryMock.Setup(x => x.GetByIdForDetailAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(movie.Id);
        result.TmdbId.Should().Be(872585);
        result.ImdbId.Should().Be("tt15398776");
        result.Title.Should().Be("Oppenheimer");
        result.Overview.Should().Be("Atom bombasının babası");
        result.Tagline.Should().Be("Dünyayı sonsuza dek değiştirdi");
        result.ReleaseYear.Should().Be(2023);
        result.RuntimeMinutes.Should().Be(180);
        result.Director.Should().Be("Christopher Nolan");
    }
}
