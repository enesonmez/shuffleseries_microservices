using Moq;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.UpdateEpisode;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes;

public class UpdateEpisodeCommandHandlerTests
{
    private readonly Mock<IEpisodeRepository> _episodeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly UpdateEpisodeCommandHandler _handler;

    public UpdateEpisodeCommandHandlerTests()
    {
        _handler = new UpdateEpisodeCommandHandler(_episodeRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_EpisodeDoesNotExist()
    {
        var command = new UpdateEpisodeCommand(Guid.NewGuid(), 2, true, 50, 9.0, "https://example.com/ep2.jpg");

        _episodeRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Episode?)null);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<EpisodeNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_UpdateEpisodeAndSaveChanges_When_ValidCommand()
    {
        var episode = new Episode(Guid.NewGuid(), 1, isStandalone: false, runtimeMinutes: 40);
        var command = new UpdateEpisodeCommand(episode.Id, 2, true, 55, 9.2, "https://example.com/still.jpg");

        _episodeRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(episode);

        await _handler.Handle(command, CancellationToken.None);

        episode.EpisodeNumber.Should().Be(2);
        episode.IsStandalone.Should().BeTrue();
        episode.RuntimeMinutes.Should().Be(55);
        episode.ImdbRating.Should().Be(9.2);
        episode.ThumbnailUrl.Should().Be("https://example.com/still.jpg");

        _episodeRepositoryMock.Verify(x => x.Update(episode), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
