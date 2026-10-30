using Moq;
using ShuffleSeries.Catalog.Application.Features.Genres.Queries.GetGenreById;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Genres;

public class GetGenreByIdQueryHandlerTests
{
    private readonly Mock<IGenreRepository> _genreRepositoryMock;
    private readonly Mock<ILanguageContext> _languageContextMock;
    private readonly GetGenreByIdQueryHandler _handler;

    public GetGenreByIdQueryHandlerTests()
    {
        _genreRepositoryMock = new Mock<IGenreRepository>();
        _languageContextMock = new Mock<ILanguageContext>();
        _languageContextMock.Setup(x => x.CurrentLanguage).Returns("tr");
        _handler = new GetGenreByIdQueryHandler(_genreRepositoryMock.Object, _languageContextMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowGenreNotFoundException_When_GenreDoesNotExist()
    {
        // Arrange
        var query = new GetGenreByIdQuery(Guid.NewGuid());
        _genreRepositoryMock.Setup(x => x.GetByIdForDetailAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Genre?)null);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<GenreNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnGenreResponse_When_GenreExists()
    {
        // Arrange
        var genre = Genre.Create("action");
        genre.AddOrUpdateTranslation("tr", "Aksiyon", "Açıklama");
        var query = new GetGenreByIdQuery(genre.Id);

        _genreRepositoryMock.Setup(x => x.GetByIdForDetailAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(genre);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(genre.Id);
        result.Code.Should().Be("action");
        result.Name.Should().Be("Aksiyon");
    }
}
