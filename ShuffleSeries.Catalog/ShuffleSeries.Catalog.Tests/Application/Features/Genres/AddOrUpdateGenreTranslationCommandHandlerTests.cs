using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Genres.Commands.Translations.AddOrUpdateGenreTranslation;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Genres;

public class AddOrUpdateGenreTranslationCommandHandlerTests
{
    private readonly Mock<IGenreRepository> _genreRepositoryMock;
    private readonly Mock<ICatalogCacheService> _cacheServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AddOrUpdateGenreTranslationCommandHandler _handler;

    public AddOrUpdateGenreTranslationCommandHandlerTests()
    {
        _genreRepositoryMock = new Mock<IGenreRepository>();
        _cacheServiceMock = new Mock<ICatalogCacheService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new AddOrUpdateGenreTranslationCommandHandler(
            _genreRepositoryMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowGenreNotFoundException_When_GenreDoesNotExist()
    {
        // Arrange
        var command = new AddOrUpdateGenreTranslationCommand(Guid.NewGuid(), "tr", "Aksiyon", "Açıklama");
        _genreRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.GenreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Genre?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<GenreNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_AddOrUpdateTranslation_And_InvalidateCache_When_Valid()
    {
        // Arrange
        var genre = Genre.Create("action");
        var command = new AddOrUpdateGenreTranslationCommand(genre.Id, "tr", "Yeni Aksiyon", "Açıklama");

        _genreRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.GenreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(genre);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        genre.Translations.Should().ContainSingle(t => t.LanguageCode == "tr" && t.Name == "Yeni Aksiyon");
        _genreRepositoryMock.Verify(x => x.Update(genre), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.RemoveAsync(CatalogCacheKeys.Genres, It.IsAny<CancellationToken>()), Times.Once);
    }
}
