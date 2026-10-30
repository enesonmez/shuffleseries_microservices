using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Platforms.Commands.CreatePlatform;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Platforms;

public class CreatePlatformCommandHandlerTests
{
    private readonly Mock<IPlatformRepository> _platformRepositoryMock;
    private readonly Mock<ICatalogCacheService> _cacheServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreatePlatformCommandHandler _handler;

    public CreatePlatformCommandHandlerTests()
    {
        _platformRepositoryMock = new Mock<IPlatformRepository>();
        _cacheServiceMock = new Mock<ICatalogCacheService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreatePlatformCommandHandler(
            _platformRepositoryMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_AddPlatform_InvalidateCache_AndReturnId()
    {
        // Arrange
        var command = new CreatePlatformCommand(
            "HBO Max",
            "hbo-max",
            "https://example.com/logo.svg",
            "https://example.com/dark.svg",
            "https://hbomax.com",
            null,
            5,
            true);

        _platformRepositoryMock.Setup(x => x.ExistsBySlugAsync(command.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _platformRepositoryMock.Verify(x => x.Add(It.Is<Platform>(p => p.Slug == "hbo-max")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.Remove(CatalogCacheKeys.Platforms), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowPlatformSlugAlreadyExistsException_When_SlugExists()
    {
        // Arrange
        var command = new CreatePlatformCommand(
            "Netflix",
            "netflix",
            "https://example.com/logo.svg",
            "https://example.com/dark.svg",
            "https://netflix.com");

        _platformRepositoryMock.Setup(x => x.ExistsBySlugAsync(command.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PlatformSlugAlreadyExistsException>();
        _cacheServiceMock.Verify(x => x.Remove(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
