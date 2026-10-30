using Moq;
using ShuffleSeries.Catalog.Application.Features.AdminMedia.Queries.GetAdminMediaList;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.AdminMedia;

public class GetAdminMediaListQueryHandlerTests
{
    private readonly Mock<IMediaRepository> _mediaRepositoryMock = new();
    private readonly Mock<ILanguageContext> _languageContextMock = new();
    private readonly GetAdminMediaListQueryHandler _handler;

    public GetAdminMediaListQueryHandlerTests()
    {
        _languageContextMock.Setup(x => x.CurrentLanguage).Returns("tr");
        _handler = new GetAdminMediaListQueryHandler(_mediaRepositoryMock.Object, _languageContextMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnPaginatedAdminMediaList()
    {
        var movie = Movie.Create(550, "tt0137523", 2023, 120, "https://example.com/poster.jpg");
        movie.AddOrUpdateTranslation("tr", "Test Film", "Özet");

        _mediaRepositoryMock.Setup(x => x.GetAdminMediaPagedListAsync(
                It.IsAny<MediaType?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([movie], 1));

        var query = new GetAdminMediaListQuery(pageNumber: 1, pageSize: 10);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().ContainSingle();
        result.Items.First().Id.Should().Be(movie.Id);
        result.Items.First().Title.Should().Be("Test Film");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenLanguageSpecifiedInRequest_Should_UseRequestedLanguage()
    {
        var movie = Movie.Create(550, "tt0137523", 2023, 120, "https://example.com/poster.jpg");
        movie.AddOrUpdateTranslation("tr", "Türkçe Başlık", "Türkçe Özet");
        movie.AddOrUpdateTranslation("en", "English Title", "English Overview");

        _mediaRepositoryMock.Setup(x => x.GetAdminMediaPagedListAsync(
                It.IsAny<MediaType?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([movie], 1));

        var query = new GetAdminMediaListQuery(language: "en");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.First().Title.Should().Be("English Title");
        result.Items.First().Overview.Should().Be("English Overview");
    }

    [Fact]
    public async Task Handle_WhenLanguageNotSpecifiedInRequest_Should_FallbackToLanguageContext()
    {
        _languageContextMock.Setup(x => x.CurrentLanguage).Returns("en");

        var movie = Movie.Create(550, "tt0137523", 2023, 120, "https://example.com/poster.jpg");
        movie.AddOrUpdateTranslation("tr", "Türkçe Başlık", "Türkçe Özet");
        movie.AddOrUpdateTranslation("en", "English Title", "English Overview");

        _mediaRepositoryMock.Setup(x => x.GetAdminMediaPagedListAsync(
                It.IsAny<MediaType?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([movie], 1));

        var query = new GetAdminMediaListQuery(language: null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.First().Title.Should().Be("English Title");
    }

    [Fact]
    public async Task Handle_WhenMediaHasPlatformsMoodsAndGenres_Should_MapNamesAndCodesCorrectly()
    {
        var movie = Movie.Create(550, "tt0137523", 2023, 120, "https://example.com/poster.jpg");
        movie.AddOrUpdateTranslation("tr", "Fight Club", "An insomniac office worker.");

        var platform = Platform.Create("Netflix", "netflix", "https://logo.png", "https://off.png", "https://netflix.com");
        var mood = Mood.Create("kafa-dagitmalik", "#FF5733", displayOrder: 1);
        var genre = Genre.Create("action", displayOrder: 1);

        movie.AddPlatform(platform.Id, "https://netflix.com/watch/550");
        movie.AddMood(mood.Id);
        movie.AddGenre(genre.Id);

        // Navigation property mapping simulation (EF Core Eager-Loading)
        typeof(MediaPlatform).GetProperty(nameof(MediaPlatform.Platform))?.SetValue(movie.MediaPlatforms.First(), platform);
        typeof(MediaMood).GetProperty(nameof(MediaMood.Mood))?.SetValue(movie.MediaMoods.First(), mood);
        typeof(MediaGenre).GetProperty(nameof(MediaGenre.Genre))?.SetValue(movie.MediaGenres.First(), genre);

        _mediaRepositoryMock.Setup(x => x.GetAdminMediaPagedListAsync(
                It.IsAny<MediaType?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([movie], 1));

        var query = new GetAdminMediaListQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        var item = result.Items.First();
        item.PlatformNames.Should().ContainSingle().Which.Should().Be("Netflix");
        item.MoodCodes.Should().ContainSingle().Which.Should().Be("kafa-dagitmalik");
        item.GenreCodes.Should().ContainSingle().Which.Should().Be("action");
    }

    [Fact]
    public async Task Handle_WhenNoTranslationExists_Should_ReturnEmptyStringsForTitleAndOverview()
    {
        var movie = Movie.Create(550, "tt0137523", 2023, 120, "https://example.com/poster.jpg");
        // Translation eklenmedi

        _mediaRepositoryMock.Setup(x => x.GetAdminMediaPagedListAsync(
                It.IsAny<MediaType?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([movie], 1));

        var query = new GetAdminMediaListQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.First().Title.Should().Be(string.Empty);
        result.Items.First().Overview.Should().Be(string.Empty);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsEmpty_Should_ReturnEmptyPaginatedList()
    {
        _mediaRepositoryMock.Setup(x => x.GetAdminMediaPagedListAsync(
                It.IsAny<MediaType?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([], 0));

        var query = new GetAdminMediaListQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_PassAllFilterAndSortParametersToRepository()
    {
        var platformId = Guid.NewGuid();
        var moodId = Guid.NewGuid();
        var genreId = Guid.NewGuid();

        _mediaRepositoryMock.Setup(x => x.GetAdminMediaPagedListAsync(
                MediaType.Movie,
                "Inception",
                platformId,
                moodId,
                genreId,
                true,
                "imdbRating",
                false,
                2,
                25,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([], 0));

        var query = new GetAdminMediaListQuery(
            mediaType: MediaType.Movie,
            search: "Inception",
            platformId: platformId,
            moodId: moodId,
            genreId: genreId,
            pageNumber: 2,
            pageSize: 25,
            isDeleted: true,
            sortBy: "imdbRating",
            sortDescending: false);

        await _handler.Handle(query, CancellationToken.None);

        _mediaRepositoryMock.Verify(x => x.GetAdminMediaPagedListAsync(
            MediaType.Movie,
            "Inception",
            platformId,
            moodId,
            genreId,
            true,
            "imdbRating",
            false,
            2,
            25,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
