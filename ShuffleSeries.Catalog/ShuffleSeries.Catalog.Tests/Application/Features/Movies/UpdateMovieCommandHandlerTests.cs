using Moq;
using ShuffleSeries.Catalog.Application.Features.Movies.Commands.UpdateMovie;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Movies;

public class UpdateMovieCommandHandlerTests
{
    private readonly Mock<IMovieRepository> _movieRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly UpdateMovieCommandHandler _handler;

    public UpdateMovieCommandHandlerTests()
    {
        _handler = new UpdateMovieCommandHandler(_movieRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_MovieDoesNotExist()
    {
        var command = new UpdateMovieCommand(Guid.NewGuid(), 550, "tt1234567", 2022, 120, "Nolan", 8.5, 1000);

        _movieRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Movie?)null);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MovieNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowDuplicateTmdbIdException_When_TmdbIdBelongsToAnotherMovie()
    {
        var movie = Movie.Create(550, "tt1234567", 2020, 110, "https://example.com/poster.jpg");
        var command = new UpdateMovieCommand(movie.Id, 999, "tt1234567", 2022, 120, "Nolan", 8.5, 1000);

        _movieRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        _movieRepositoryMock.Setup(x => x.ExistsByTmdbIdAsync(999, movie.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<DuplicateTmdbIdException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowDuplicateImdbIdException_When_ImdbIdBelongsToAnotherMovie()
    {
        var movie = Movie.Create(550, "tt1234567", 2020, 110, "https://example.com/poster.jpg");
        var command = new UpdateMovieCommand(movie.Id, 550, "tt9999999", 2022, 120, "Nolan", 8.5, 1000);

        _movieRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        _movieRepositoryMock.Setup(x => x.ExistsByTmdbIdAsync(550, movie.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _movieRepositoryMock.Setup(x => x.ExistsByImdbIdAsync("tt9999999", movie.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<DuplicateImdbIdException>();
    }

    [Fact]
    public async Task Handle_Should_UpdateMovieDetails_When_ValidCommand()
    {
        var movie = Movie.Create(550, "tt1234567", 2020, 110, "https://example.com/poster.jpg");
        var command = new UpdateMovieCommand(movie.Id, 550, "tt1234567", 2022, 130, "Nolan", 8.9, 5000);

        _movieRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        _movieRepositoryMock.Setup(x => x.ExistsByTmdbIdAsync(550, movie.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _movieRepositoryMock.Setup(x => x.ExistsByImdbIdAsync("tt1234567", movie.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _handler.Handle(command, CancellationToken.None);

        movie.ReleaseYear.Should().Be(2022);
        movie.RuntimeMinutes.Should().Be(130);
        movie.Director.Should().Be("Nolan");
        movie.ImdbId.Should().Be("tt1234567");
        movie.ImdbRating.Should().Be(8.9);
        movie.ImdbVotes.Should().Be(5000);

        _movieRepositoryMock.Verify(x => x.Update(movie), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
