using Moq;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Moods.RemoveMediaMood;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.MediaAssociations.Commands.Moods.RemoveMediaMood;

public class RemoveMediaMoodCommandHandlerTests
{
    private readonly Mock<IMediaRepository> _mediaRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task Handle_ShouldRemoveMoodAndSave_WhenMediaExists()
    {
        var handler = new RemoveMediaMoodCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var movie = Movie.Create(550, "tt0137523", 2020, 100, "https://example.com/p.jpg");
        var moodId = Guid.NewGuid();
        movie.AddMood(moodId);

        var command = new RemoveMediaMoodCommand(movie.Id, moodId);

        _mediaRepositoryMock.Setup(x => x.GetByIdWithMoodsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        await handler.Handle(command, CancellationToken.None);

        movie.MediaMoods.Should().BeEmpty();
        _mediaRepositoryMock.Verify(x => x.Update(movie), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowMediaNotFoundException_WhenMediaDoesNotExist()
    {
        var handler = new RemoveMediaMoodCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var command = new RemoveMediaMoodCommand(Guid.NewGuid(), Guid.NewGuid());

        _mediaRepositoryMock.Setup(x => x.GetByIdWithMoodsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Media?)null);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MediaNotFoundException>();
    }
}
