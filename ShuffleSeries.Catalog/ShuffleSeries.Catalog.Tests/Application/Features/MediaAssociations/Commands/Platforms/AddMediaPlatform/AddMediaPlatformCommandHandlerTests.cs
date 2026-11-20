using Moq;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Platforms.AddMediaPlatform;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.MediaAssociations.Commands.Platforms.AddMediaPlatform;

public class AddMediaPlatformCommandHandlerTests
{
    private readonly Mock<IMediaRepository> _mediaRepositoryMock = new();
    private readonly Mock<IPlatformRepository> _platformRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task Handle_ShouldThrowPlatformNotFoundException_WhenPlatformDoesNotExist()
    {
        var handler = new AddMediaPlatformCommandHandler(_mediaRepositoryMock.Object, _platformRepositoryMock.Object, _unitOfWorkMock.Object);
        var movie = Movie.Create(550, "tt0137523", 2020, 100, "https://example.com/p.jpg");
        var command = new AddMediaPlatformCommand(movie.Id, Guid.NewGuid(), "https://netflix.com/title/123", null);

        _mediaRepositoryMock.Setup(x => x.GetByIdWithPlatformsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        _platformRepositoryMock.Setup(x => x.GetByIdAsync(command.PlatformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Platform?)null);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<PlatformNotFoundException>();
    }
}
