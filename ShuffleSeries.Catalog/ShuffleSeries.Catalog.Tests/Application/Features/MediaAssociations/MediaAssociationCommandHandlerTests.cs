using Moq;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Genres.AddMediaGenre;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Genres.RemoveMediaGenre;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Moods.AddMediaMood;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Moods.RemoveMediaMood;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Platforms.AddMediaPlatform;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Platforms.RemoveMediaPlatform;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Translations.AddOrUpdateMediaTranslation;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Translations.RemoveMediaTranslation;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Tests.Application.Features.MediaAssociations;

public class MediaAssociationCommandHandlerTests
{
    private readonly Mock<IMediaRepository> _mediaRepositoryMock = new();
    private readonly Mock<IPlatformRepository> _platformRepositoryMock = new();
    private readonly Mock<IMoodRepository> _moodRepositoryMock = new();
    private readonly Mock<IGenreRepository> _genreRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    #region Add Operations

    [Fact]
    public async Task AddOrUpdateTranslation_Should_ThrowMediaNotFoundException_When_MediaDoesNotExist()
    {
        var handler = new AddOrUpdateMediaTranslationCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var command = new AddOrUpdateMediaTranslationCommand(Guid.NewGuid(), "tr", "Yeni Başlık", "Özet", "Slogan");

        _mediaRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Media?)null);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task AddOrUpdateTranslation_Should_AddTranslationAndSave_When_MediaExists()
    {
        var handler = new AddOrUpdateMediaTranslationCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var movie = Movie.Create(550, "tt0137523", 2020, 100, "https://example.com/p.jpg");
        var command = new AddOrUpdateMediaTranslationCommand(movie.Id, "tr", "Yeni Başlık", "Özet", "Slogan");

        _mediaRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        await handler.Handle(command, CancellationToken.None);

        movie.Translations.Should().ContainSingle(t => t.Title == "Yeni Başlık" && t.LanguageCode.Value == "tr");
        _mediaRepositoryMock.Verify(x => x.Update(movie), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddMediaPlatform_Should_ThrowPlatformNotFoundException_When_PlatformDoesNotExist()
    {
        var handler = new AddMediaPlatformCommandHandler(_mediaRepositoryMock.Object, _platformRepositoryMock.Object, _unitOfWorkMock.Object);
        var movie = Movie.Create(550, "tt0137523", 2020, 100, "https://example.com/p.jpg");
        var command = new AddMediaPlatformCommand(movie.Id, Guid.NewGuid(), "https://netflix.com/title/123", null);

        _mediaRepositoryMock.Setup(x => x.GetByIdWithPlatformsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        _platformRepositoryMock.Setup(x => x.GetByIdAsync(command.PlatformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Platform?)null);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<PlatformNotFoundException>();
    }

    [Fact]
    public async Task AddMediaMood_Should_ThrowMoodNotFoundException_When_MoodDoesNotExist()
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

    [Fact]
    public async Task AddMediaGenre_Should_ThrowGenreNotFoundException_When_GenreDoesNotExist()
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

    #endregion

    #region Remove Operations

    [Fact]
    public async Task RemoveMediaPlatform_Should_RemovePlatformAndSave_When_MediaExists()
    {
        var handler = new RemoveMediaPlatformCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var movie = Movie.Create(550, "tt0137523", 2020, 100, "https://example.com/p.jpg");
        var platformId = Guid.NewGuid();
        movie.AddPlatform(platformId, "https://netflix.com/watch");

        var command = new RemoveMediaPlatformCommand(movie.Id, platformId);

        _mediaRepositoryMock.Setup(x => x.GetByIdWithPlatformsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        await handler.Handle(command, CancellationToken.None);

        movie.MediaPlatforms.Should().BeEmpty();
        _mediaRepositoryMock.Verify(x => x.Update(movie), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveMediaPlatform_Should_ThrowMediaNotFoundException_When_MediaDoesNotExist()
    {
        var handler = new RemoveMediaPlatformCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var command = new RemoveMediaPlatformCommand(Guid.NewGuid(), Guid.NewGuid());

        _mediaRepositoryMock.Setup(x => x.GetByIdWithPlatformsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Media?)null);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task RemoveMediaMood_Should_RemoveMoodAndSave_When_MediaExists()
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
    public async Task RemoveMediaMood_Should_ThrowMediaNotFoundException_When_MediaDoesNotExist()
    {
        var handler = new RemoveMediaMoodCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var command = new RemoveMediaMoodCommand(Guid.NewGuid(), Guid.NewGuid());

        _mediaRepositoryMock.Setup(x => x.GetByIdWithMoodsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Media?)null);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task RemoveMediaGenre_Should_RemoveGenreAndSave_When_MediaExists()
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
    public async Task RemoveMediaGenre_Should_ThrowMediaNotFoundException_When_MediaDoesNotExist()
    {
        var handler = new RemoveMediaGenreCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var command = new RemoveMediaGenreCommand(Guid.NewGuid(), Guid.NewGuid());

        _mediaRepositoryMock.Setup(x => x.GetByIdWithGenresAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Media?)null);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task RemoveMediaTranslation_Should_RemoveTranslationAndSave_When_MediaExists()
    {
        var handler = new RemoveMediaTranslationCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var movie = Movie.Create(550, "tt0137523", 2020, 100, "https://example.com/p.jpg");
        movie.AddOrUpdateTranslation(LanguageCode.Turkish, "Türkçe", "Özet");
        movie.AddOrUpdateTranslation(LanguageCode.English, "English", "Overview");

        var command = new RemoveMediaTranslationCommand(movie.Id, "en");

        _mediaRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        await handler.Handle(command, CancellationToken.None);

        movie.Translations.Should().NotContain(t => t.LanguageCode == LanguageCode.English);
        _mediaRepositoryMock.Verify(x => x.Update(movie), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveMediaTranslation_Should_ThrowMediaNotFoundException_When_MediaDoesNotExist()
    {
        var handler = new RemoveMediaTranslationCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var command = new RemoveMediaTranslationCommand(Guid.NewGuid(), "tr");

        _mediaRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Media?)null);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MediaNotFoundException>();
    }

    #endregion
}
