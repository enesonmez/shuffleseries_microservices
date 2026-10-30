using Moq;
using ShuffleSeries.Catalog.Application.Features.Movies.Commands.DeleteMovie;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Movies;

public class DeleteMovieCommandHandlerTests
{
    private readonly Mock<IMovieRepository> _movieRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly DeleteMovieCommandHandler _handler;

    public DeleteMovieCommandHandlerTests()
    {
        _handler = new DeleteMovieCommandHandler(_movieRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_MovieDoesNotExist()
    {
        var command = new DeleteMovieCommand(Guid.NewGuid());

        _movieRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Movie?)null);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MovieNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_DeleteMovieAndSaveChanges_When_MovieExists()
    {
        var movie = Movie.Create(550, "tt0137523", 2021, 120, "https://example.com/poster.jpg");
        var command = new DeleteMovieCommand(movie.Id);

        _movieRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        await _handler.Handle(command, CancellationToken.None);

        movie.IsDeleted.Should().BeTrue();
        _movieRepositoryMock.Verify(x => x.Delete(movie), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
