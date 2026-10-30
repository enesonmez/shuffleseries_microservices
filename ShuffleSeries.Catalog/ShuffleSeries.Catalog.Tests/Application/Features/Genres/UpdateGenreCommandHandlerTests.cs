using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Genres.Commands.UpdateGenre;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Genres;

public class UpdateGenreCommandHandlerTests
{
    private readonly Mock<IGenreRepository> _genreRepositoryMock = new();
    private readonly Mock<ICatalogCacheService> _cacheServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly UpdateGenreCommandHandler _handler;

    public UpdateGenreCommandHandlerTests()
    {
        _handler = new UpdateGenreCommandHandler(
            _genreRepositoryMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_GenreDoesNotExist()
    {
        var command = new UpdateGenreCommand(Guid.NewGuid(), "action", 1);

        _genreRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Genre?)null);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<GenreNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowGenreCodeAlreadyExistsException_When_CodeChangedAndExists()
    {
        var genre = Genre.Create("action", 1);
        var command = new UpdateGenreCommand(genre.Id, "drama", 2);

        _genreRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(genre);

        _genreRepositoryMock.Setup(x => x.ExistsByCodeAsync("drama", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<GenreCodeAlreadyExistsException>();
    }

    [Fact]
    public async Task Handle_Should_UpdateGenreAndInvalidateCache_When_Valid()
    {
        var genre = Genre.Create("action", 1);
        var command = new UpdateGenreCommand(genre.Id, "action-thriller", 5);

        _genreRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(genre);

        _genreRepositoryMock.Setup(x => x.ExistsByCodeAsync("action-thriller", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _handler.Handle(command, CancellationToken.None);

        genre.Code.Should().Be("action-thriller");
        genre.DisplayOrder.Should().Be(5);

        _genreRepositoryMock.Verify(x => x.Update(genre), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.RemoveAsync(CatalogCacheKeys.Genres, It.IsAny<CancellationToken>()), Times.Once);
    }
}
