using Moq;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Genres.AddMediaGenre;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.MediaAssociations.Commands.Genres.AddMediaGenre;

public class AddMediaGenreCommandHandlerTests
{
    private readonly Mock<IMediaRepository> _mediaRepositoryMock = new();
    private readonly Mock<IGenreRepository> _genreRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task Handle_ShouldThrowGenreNotFoundException_WhenGenreDoesNotExist()
    {
        var handler = new AddMediaGenreCommandHandler(_mediaRepositoryMock.Object, _genreRepositoryMock.Object, _unitOfWorkMock.Object);
        var movie = Movie.Create(550, "tt0137523", 2020, 100, "https://example.com/p.jpg");
        var command = new AddMediaGenreCommand(movie.Id, Guid.NewGuid());

        _mediaRepositoryMock.Setup(x => x.GetByIdWithGenresAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        _genreRepositoryMock.Setup(x => x.GetByIdAsync(command.GenreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Genre?)null);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<GenreNotFoundException>();
    }
}
