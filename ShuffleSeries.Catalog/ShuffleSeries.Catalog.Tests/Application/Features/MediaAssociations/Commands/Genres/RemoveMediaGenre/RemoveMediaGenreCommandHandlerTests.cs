using Moq;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Genres.RemoveMediaGenre;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.MediaAssociations.Commands.Genres.RemoveMediaGenre;

public class RemoveMediaGenreCommandHandlerTests
{
    private readonly Mock<IMediaRepository> _mediaRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task Handle_ShouldRemoveGenreAndSave_WhenMediaExists()
    {
        var handler = new RemoveMediaGenreCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var movie = Movie.Create(550, "tt0137523", 2020, 100, "https://example.com/p.jpg");
        var genreId = Guid.NewGuid();
        movie.AddGenre(genreId);

        var command = new RemoveMediaGenreCommand(movie.Id, genreId);

        _mediaRepositoryMock.Setup(x => x.GetByIdWithGenresAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        await handler.Handle(command, CancellationToken.None);

        movie.MediaGenres.Should().BeEmpty();
        _mediaRepositoryMock.Verify(x => x.Update(movie), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowMediaNotFoundException_WhenMediaDoesNotExist()
    {
        var handler = new RemoveMediaGenreCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var command = new RemoveMediaGenreCommand(Guid.NewGuid(), Guid.NewGuid());

        _mediaRepositoryMock.Setup(x => x.GetByIdWithGenresAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Media?)null);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MediaNotFoundException>();
    }
}
