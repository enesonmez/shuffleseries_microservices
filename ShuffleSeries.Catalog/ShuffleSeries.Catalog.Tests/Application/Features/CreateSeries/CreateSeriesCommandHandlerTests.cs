using Moq;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.CreateSeries;

public class CreateSeriesCommandHandlerTests
{
    private readonly Mock<ISeriesRepository> _seriesRepositoryMock;
    private readonly Mock<IPlatformRepository> _platformRepositoryMock;
    private readonly Mock<IMoodRepository> _moodRepositoryMock;
    private readonly Mock<IGenreRepository> _genreRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateSeriesCommandHandler _handler;

    public CreateSeriesCommandHandlerTests()
    {
        _seriesRepositoryMock = new Mock<ISeriesRepository>();
        _platformRepositoryMock = new Mock<IPlatformRepository>();
        _moodRepositoryMock = new Mock<IMoodRepository>();
        _genreRepositoryMock = new Mock<IGenreRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _platformRepositoryMock.Setup(x => x.GetActivePlatformsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Platform>());
        _moodRepositoryMock.Setup(x => x.GetActiveMoodsWithTranslationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Mood>());
        _genreRepositoryMock.Setup(x => x.GetActiveGenresWithTranslationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Genre>());

        _handler = new CreateSeriesCommandHandler(
            _seriesRepositoryMock.Object,
            _platformRepositoryMock.Object,
            _moodRepositoryMock.Object,
            _genreRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CreateSeriesAndReturnId_When_Valid()
    {
        // Arrange
        var command = new CreateSeriesCommand(
            1405,
            "tt0773262",
            2006,
            SeriesType.Standard,
            "https://example.com/dexter.jpg",
            "https://example.com/dexter-bg.jpg",
            "tr",
            "Dexter",
            "Serial killer analyst",
            "America's favorite serial killer",
            8.7,
            750000,
            8);

        _seriesRepositoryMock.Setup(x => x.ExistsByTmdbIdAsync(command.TmdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _seriesRepositoryMock.Setup(x => x.ExistsByImdbIdAsync(command.ImdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();

        _seriesRepositoryMock.Verify(x => x.Add(It.Is<Series>(s => s.Translations.Any(t => t.Title == command.Title))), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowDuplicateTmdbIdException_When_TmdbIdIsNotUnique()
    {
        // Arrange
        var command = new CreateSeriesCommand(
            1405,
            "tt0773262",
            2006,
            SeriesType.Standard,
            "https://example.com/dexter.jpg",
            "https://example.com/dexter-bg.jpg",
            "tr",
            "Dexter",
            "Serial killer analyst",
            "America's favorite serial killer",
            8.7,
            750000,
            8);

        _seriesRepositoryMock.Setup(x => x.ExistsByTmdbIdAsync(command.TmdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<DuplicateTmdbIdException>();

        _seriesRepositoryMock.Verify(x => x.Add(It.IsAny<Series>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowDuplicateImdbIdException_When_ImdbIdIsNotUnique()
    {
        // Arrange
        var command = new CreateSeriesCommand(
            1405,
            "tt0773262",
            2006,
            SeriesType.Standard,
            "https://example.com/dexter.jpg",
            "https://example.com/dexter-bg.jpg",
            "tr",
            "Dexter",
            "Serial killer analyst",
            "America's favorite serial killer",
            8.7,
            750000,
            8);

        _seriesRepositoryMock.Setup(x => x.ExistsByTmdbIdAsync(command.TmdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _seriesRepositoryMock.Setup(x => x.ExistsByImdbIdAsync("tt0773262", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<DuplicateImdbIdException>();

        _seriesRepositoryMock.Verify(x => x.Add(It.IsAny<Series>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_AssociatePlatformsMoodsGenres_When_Provided()
    {
        // Arrange
        var platform = Platform.Create("Netflix", "netflix", "https://logo.png", "https://inactive.png", "https://netflix.com");
        var mood = Mood.Create("kafa-dagitmalik", "#FF0000");
        mood.AddOrUpdateTranslation("tr", "Kafa Dağıtmalık", "Açıklama");
        var genre = Genre.Create("action");
        genre.AddOrUpdateTranslation("tr", "Aksiyon");

        _platformRepositoryMock.Setup(x => x.GetActivePlatformsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Platform> { platform });
        _moodRepositoryMock.Setup(x => x.GetActiveMoodsWithTranslationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Mood> { mood });
        _genreRepositoryMock.Setup(x => x.GetActiveGenresWithTranslationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Genre> { genre });

        var command = new CreateSeriesCommand(
            1405,
            "tt0773262",
            2006,
            SeriesType.Standard,
            "https://example.com/dexter.jpg",
            "https://example.com/dexter-bg.jpg",
            "tr",
            "Dexter",
            "Serial killer analyst",
            "America's favorite serial killer",
            8.7,
            750000,
            8,
            PlatformIds: new[] { platform.Id },
            MoodIds: new[] { mood.Id },
            GenreIds: new[] { genre.Id });

        _seriesRepositoryMock.Setup(x => x.ExistsByTmdbIdAsync(command.TmdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _seriesRepositoryMock.Setup(x => x.ExistsByImdbIdAsync(command.ImdbId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _seriesRepositoryMock.Verify(x => x.Add(It.Is<Series>(s =>
            s.MediaPlatforms.Any(p => p.PlatformId == platform.Id) &&
            s.MediaMoods.Any(m => m.MoodId == mood.Id) &&
            s.MediaGenres.Any(g => g.GenreId == genre.Id))), Times.Once);
    }
}
