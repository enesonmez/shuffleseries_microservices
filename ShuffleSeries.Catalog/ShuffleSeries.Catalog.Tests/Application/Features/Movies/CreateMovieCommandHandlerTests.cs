using Moq;
using ShuffleSeries.Catalog.Application.Features.Movies.Commands.CreateMovie;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Movies;

public class CreateMovieCommandHandlerTests
{
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly Mock<IPlatformRepository> _platformRepositoryMock;
    private readonly Mock<IMoodRepository> _moodRepositoryMock;
    private readonly Mock<IGenreRepository> _genreRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateMovieCommandHandler _handler;

    public CreateMovieCommandHandlerTests()
    {
        _movieRepositoryMock = new Mock<IMovieRepository>();
        _platformRepositoryMock = new Mock<IPlatformRepository>();
        _moodRepositoryMock = new Mock<IMoodRepository>();
        _genreRepositoryMock = new Mock<IGenreRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CreateMovieCommandHandler(
            _movieRepositoryMock.Object,
            _platformRepositoryMock.Object,
            _moodRepositoryMock.Object,
            _genreRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CreateMovieAndReturnId_When_Valid()
    {
        // Arrange
        var command = new CreateMovieCommand(
            872585,
            "tt15398776",
            2023,
            180,
            "https://example.com/oppenheimer.jpg",
            null,
            "tr",
            "Oppenheimer",
            "Atom bombasının babası.",
            "Dünyayı değiştiren adam",
            "Christopher Nolan",
            8.9,
            700000);

        _movieRepositoryMock.Setup(x => x.ExistsByTmdbIdAsync(command.TmdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _movieRepositoryMock.Setup(x => x.ExistsByImdbIdAsync(command.ImdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _movieRepositoryMock.Verify(x => x.Add(It.Is<Movie>(m => m.ReleaseYear == 2023 && m.TmdbId == 872585)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowDuplicateTmdbIdException_When_TmdbIdExists()
    {
        // Arrange
        var command = new CreateMovieCommand(
            872585,
            "tt15398776",
            2023,
            180,
            "https://example.com/oppenheimer.jpg",
            null,
            "tr",
            "Oppenheimer",
            "Atom bombasının babası.");

        _movieRepositoryMock.Setup(x => x.ExistsByTmdbIdAsync(command.TmdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DuplicateTmdbIdException>();
        _movieRepositoryMock.Verify(x => x.Add(It.IsAny<Movie>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowDuplicateImdbIdException_When_ImdbIdExists()
    {
        // Arrange
        var command = new CreateMovieCommand(
            872585,
            "tt15398776",
            2023,
            180,
            "https://example.com/oppenheimer.jpg",
            null,
            "tr",
            "Oppenheimer",
            "Atom bombasının babası.");

        _movieRepositoryMock.Setup(x => x.ExistsByTmdbIdAsync(command.TmdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _movieRepositoryMock.Setup(x => x.ExistsByImdbIdAsync("tt15398776", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DuplicateImdbIdException>();
        _movieRepositoryMock.Verify(x => x.Add(It.IsAny<Movie>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowPlatformNotFoundException_When_PlatformDoesNotExist()
    {
        // Arrange
        var missingPlatformId = Guid.NewGuid();
        var command = new CreateMovieCommand(
            872585,
            "tt15398776",
            2023,
            180,
            "https://example.com/oppenheimer.jpg",
            null,
            "tr",
            "Oppenheimer",
            "Atom bombasının babası.",
            PlatformIds: [missingPlatformId]);

        _movieRepositoryMock.Setup(x => x.ExistsByTmdbIdAsync(command.TmdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _movieRepositoryMock.Setup(x => x.ExistsByImdbIdAsync(command.ImdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _platformRepositoryMock.Setup(x => x.GetActivePlatformsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PlatformNotFoundException>();
        _platformRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowMoodNotFoundException_When_MoodDoesNotExist()
    {
        // Arrange
        var missingMoodId = Guid.NewGuid();
        var command = new CreateMovieCommand(
            872585,
            "tt15398776",
            2023,
            180,
            "https://example.com/oppenheimer.jpg",
            null,
            "tr",
            "Oppenheimer",
            "Atom bombasının babası.",
            MoodIds: [missingMoodId]);

        _movieRepositoryMock.Setup(x => x.ExistsByTmdbIdAsync(command.TmdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _movieRepositoryMock.Setup(x => x.ExistsByImdbIdAsync(command.ImdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _moodRepositoryMock.Setup(x => x.GetActiveMoodsWithTranslationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MoodNotFoundException>();
        _moodRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowGenreNotFoundException_When_GenreDoesNotExist()
    {
        // Arrange
        var missingGenreId = Guid.NewGuid();
        var command = new CreateMovieCommand(
            872585,
            "tt15398776",
            2023,
            180,
            "https://example.com/oppenheimer.jpg",
            null,
            "tr",
            "Oppenheimer",
            "Atom bombasının babası.",
            GenreIds: [missingGenreId]);

        _movieRepositoryMock.Setup(x => x.ExistsByTmdbIdAsync(command.TmdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _movieRepositoryMock.Setup(x => x.ExistsByImdbIdAsync(command.ImdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _genreRepositoryMock.Setup(x => x.GetActiveGenresWithTranslationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<GenreNotFoundException>();
        _genreRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
