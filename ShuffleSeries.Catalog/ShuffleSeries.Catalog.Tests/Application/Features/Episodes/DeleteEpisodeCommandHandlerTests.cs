using Moq;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.DeleteEpisode;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes;

public class DeleteEpisodeCommandHandlerTests
{
    private readonly Mock<IEpisodeRepository> _episodeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly DeleteEpisodeCommandHandler _handler;

    public DeleteEpisodeCommandHandlerTests()
    {
        _handler = new DeleteEpisodeCommandHandler(_episodeRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_EpisodeDoesNotExist()
    {
        var command = new DeleteEpisodeCommand(Guid.NewGuid());

        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Episode?)null);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<EpisodeNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_DeleteEpisodeAndSaveChanges_When_EpisodeExists()
    {
        var episode = new Episode(Guid.NewGuid(), 1, isStandalone: true);
        var command = new DeleteEpisodeCommand(episode.Id);

        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(episode);

        await _handler.Handle(command, CancellationToken.None);

        episode.IsDeleted.Should().BeTrue();
        _episodeRepositoryMock.Verify(x => x.Delete(episode), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
