using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Genres.Commands.DeleteGenre;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Genres;

public class DeleteGenreCommandHandlerTests
{
    private readonly Mock<IGenreRepository> _genreRepositoryMock = new();
    private readonly Mock<ICatalogCacheService> _cacheServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly DeleteGenreCommandHandler _handler;

    public DeleteGenreCommandHandlerTests()
    {
        _handler = new DeleteGenreCommandHandler(
            _genreRepositoryMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_GenreDoesNotExist()
    {
        var command = new DeleteGenreCommand(Guid.NewGuid());

        _genreRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Genre?)null);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<GenreNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_DeleteGenreAndInvalidateCache_When_GenreExists()
    {
        var genre = Genre.Create("action", 1);
        var command = new DeleteGenreCommand(genre.Id);

        _genreRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(genre);

        await _handler.Handle(command, CancellationToken.None);

        genre.IsDeleted.Should().BeTrue();
        _genreRepositoryMock.Verify(x => x.Delete(genre), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.Remove(CatalogCacheKeys.Genres), Times.Once);
    }
}
