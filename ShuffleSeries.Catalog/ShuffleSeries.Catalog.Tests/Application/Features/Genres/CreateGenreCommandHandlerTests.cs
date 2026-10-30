using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Genres.Commands.CreateGenre;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Genres;

public class CreateGenreCommandHandlerTests
{
    private readonly Mock<IGenreRepository> _genreRepositoryMock = new();
    private readonly Mock<ICatalogCacheService> _cacheServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CreateGenreCommandHandler _handler;

    public CreateGenreCommandHandlerTests()
    {
        _handler = new CreateGenreCommandHandler(
            _genreRepositoryMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowGenreCodeAlreadyExistsException_When_CodeExists()
    {
        var command = new CreateGenreCommand(
            Code: "sci-fi",
            Name: "Science Fiction",
            LanguageCode: "en",
            Description: "Futuristic stories",
            DisplayOrder: 1,
            IsActive: true);

        _genreRepositoryMock.Setup(x => x.ExistsByCodeAsync(command.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<GenreCodeAlreadyExistsException>();
    }

    [Fact]
    public async Task Handle_Should_CreateGenre_And_InvalidateCache_When_Valid()
    {
        var command = new CreateGenreCommand(
            Code: "sci-fi",
            Name: "Science Fiction",
            LanguageCode: "en",
            Description: "Futuristic stories",
            DisplayOrder: 1,
            IsActive: true);

        _genreRepositoryMock.Setup(x => x.ExistsByCodeAsync(command.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _genreRepositoryMock.Verify(x => x.Add(It.Is<Genre>(g => g.Code == "sci-fi")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.RemoveAsync(CatalogCacheKeys.Genres, It.IsAny<CancellationToken>()), Times.Once);
    }
}
