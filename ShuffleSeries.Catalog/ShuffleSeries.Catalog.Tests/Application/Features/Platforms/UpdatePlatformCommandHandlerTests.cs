using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Platforms.Commands.UpdatePlatform;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Platforms;

public class UpdatePlatformCommandHandlerTests
{
    private readonly Mock<IPlatformRepository> _platformRepositoryMock;
    private readonly Mock<ICatalogCacheService> _cacheServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdatePlatformCommandHandler _handler;

    public UpdatePlatformCommandHandlerTests()
    {
        _platformRepositoryMock = new Mock<IPlatformRepository>();
        _cacheServiceMock = new Mock<ICatalogCacheService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new UpdatePlatformCommandHandler(
            _platformRepositoryMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPlatformExists_ShouldUpdate_Save_AndInvalidateCache()
    {
        // Arrange
        var platformId = Guid.NewGuid();
        var platform = Platform.Create("Netflix", "netflix", "logo1", "inact1", "https://netflix.com");
        var command = new UpdatePlatformCommand(
            platformId,
            "Netflix Updated",
            "netflix-updated",
            "https://example.com/logo2.svg",
            "https://example.com/inact2.svg",
            "https://netflix.com/tr",
            "netflix://",
            2);

        _platformRepositoryMock.Setup(x => x.GetByIdAsync(platformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        _platformRepositoryMock.Setup(x => x.ExistsBySlugAsync("netflix-updated", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        platform.Name.Should().Be("Netflix Updated");
        platform.Slug.Should().Be("netflix-updated");
        platform.BaseAppSchemeUrl.Should().Be("netflix://");
        _platformRepositoryMock.Verify(x => x.Update(platform), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.RemoveAsync(CatalogCacheKeys.Platforms, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSlugNotChanged_ShouldNotCheckSlugConflict()
    {
        // Arrange
        var platformId = Guid.NewGuid();
        var platform = Platform.Create("Netflix", "netflix", "logo1", "inact1", "https://netflix.com");
        var command = new UpdatePlatformCommand(
            platformId,
            "Netflix Rebranded",
            "netflix",
            "https://example.com/logo2.svg",
            "https://example.com/inact2.svg",
            "https://netflix.com/tr",
            null,
            1);

        _platformRepositoryMock.Setup(x => x.GetByIdAsync(platformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _platformRepositoryMock.Verify(x => x.ExistsBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _platformRepositoryMock.Verify(x => x.Update(platform), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlatformNotFound_ShouldThrowPlatformNotFoundException()
    {
        // Arrange
        var platformId = Guid.NewGuid();
        var command = new UpdatePlatformCommand(
            platformId,
            "Netflix",
            "netflix",
            "https://example.com/logo.svg",
            "https://example.com/inact.svg",
            "https://netflix.com",
            null,
            0);

        _platformRepositoryMock.Setup(x => x.GetByIdAsync(platformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Platform?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PlatformNotFoundException>();
        _platformRepositoryMock.Verify(x => x.Update(It.IsAny<Platform>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNewSlugAlreadyExists_ShouldThrowPlatformSlugAlreadyExistsException()
    {
        // Arrange
        var platformId = Guid.NewGuid();
        var platform = Platform.Create("Netflix", "netflix", "logo1", "inact1", "https://netflix.com");
        var command = new UpdatePlatformCommand(
            platformId,
            "Netflix Prime",
            "prime-video",
            "https://example.com/logo.svg",
            "https://example.com/inact.svg",
            "https://netflix.com",
            null,
            0);

        _platformRepositoryMock.Setup(x => x.GetByIdAsync(platformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        _platformRepositoryMock.Setup(x => x.ExistsBySlugAsync("prime-video", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PlatformSlugAlreadyExistsException>();
        _platformRepositoryMock.Verify(x => x.Update(It.IsAny<Platform>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
