using Moq;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Moods.AddMediaMood;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.MediaAssociations.Commands.Moods.AddMediaMood;

public class AddMediaMoodCommandHandlerTests
{
    private readonly Mock<IMediaRepository> _mediaRepositoryMock = new();
    private readonly Mock<IMoodRepository> _moodRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task Handle_ShouldThrowMoodNotFoundException_WhenMoodDoesNotExist()
    {
        var handler = new AddMediaMoodCommandHandler(_mediaRepositoryMock.Object, _moodRepositoryMock.Object, _unitOfWorkMock.Object);
        var movie = Movie.Create(550, "tt0137523", 2020, 100, "https://example.com/p.jpg");
        var command = new AddMediaMoodCommand(movie.Id, Guid.NewGuid());

        _mediaRepositoryMock.Setup(x => x.GetByIdWithMoodsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        _moodRepositoryMock.Setup(x => x.GetByIdAsync(command.MoodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mood?)null);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MoodNotFoundException>();
    }
}
