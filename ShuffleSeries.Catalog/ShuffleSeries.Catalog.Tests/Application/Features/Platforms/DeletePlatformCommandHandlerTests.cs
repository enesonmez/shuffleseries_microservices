using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Platforms.Commands.DeletePlatform;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Platforms;

public class DeletePlatformCommandHandlerTests
{
    private readonly Mock<IPlatformRepository> _platformRepositoryMock;
    private readonly Mock<ICatalogCacheService> _cacheServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeletePlatformCommandHandler _handler;

    public DeletePlatformCommandHandlerTests()
    {
        _platformRepositoryMock = new Mock<IPlatformRepository>();
        _cacheServiceMock = new Mock<ICatalogCacheService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new DeletePlatformCommandHandler(
            _platformRepositoryMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPlatformExists_ShouldDelete_Save_AndInvalidateCache()
    {
        // Arrange
        var platformId = Guid.NewGuid();
        var platform = Platform.Create("Netflix", "netflix", "logo", "inact", "https://netflix.com");
        var command = new DeletePlatformCommand(platformId);

        _platformRepositoryMock.Setup(x => x.GetByIdAsync(platformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        platform.IsDeleted.Should().BeTrue();
        _platformRepositoryMock.Verify(x => x.Delete(platform), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.Remove(CatalogCacheKeys.Platforms), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlatformNotFound_ShouldThrowPlatformNotFoundException()
    {
        // Arrange
        var platformId = Guid.NewGuid();
        var command = new DeletePlatformCommand(platformId);

        _platformRepositoryMock.Setup(x => x.GetByIdAsync(platformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Platform?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PlatformNotFoundException>();
        _platformRepositoryMock.Verify(x => x.Delete(It.IsAny<Platform>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(x => x.Remove(It.IsAny<string>()), Times.Never);
    }
}
