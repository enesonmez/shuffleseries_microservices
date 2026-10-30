using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Tests.Domain.Exceptions;

public class CatalogExceptionsTests
{
    [Fact]
    public void MediaNotFoundException_ShouldHaveCorrectErrorCodeAndStatus()
    {
        var id = Guid.NewGuid();
        var ex = new MediaNotFoundException(id);

        ex.Should().BeAssignableTo<NotFoundException>();
        ex.Code.Should().Be("MEDIA_NOT_FOUND");
        ex.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void SeriesNotFoundException_ShouldHaveCorrectErrorCode()
    {
        var id = Guid.NewGuid();
        var ex = new SeriesNotFoundException(id);

        ex.Should().BeAssignableTo<NotFoundException>();
        ex.Code.Should().Be("SERIES_NOT_FOUND");
    }

    [Fact]
    public void MovieNotFoundException_ShouldHaveCorrectErrorCode()
    {
        var id = Guid.NewGuid();
        var ex = new MovieNotFoundException(id);

        ex.Should().BeAssignableTo<NotFoundException>();
        ex.Code.Should().Be("MOVIE_NOT_FOUND");
    }

    [Fact]
    public void PlatformSlugAlreadyExistsException_ShouldHaveCorrectErrorCode()
    {
        var ex = new PlatformSlugAlreadyExistsException("netflix");

        ex.Should().BeAssignableTo<ConflictException>();
        ex.Code.Should().Be("PLATFORM_SLUG_ALREADY_EXISTS");
    }

    [Fact]
    public void MoodCodeAlreadyExistsException_ShouldHaveCorrectErrorCode()
    {
        var ex = new MoodCodeAlreadyExistsException("beyin-yakanlar");

        ex.Should().BeAssignableTo<ConflictException>();
        ex.Code.Should().Be("MOOD_CODE_ALREADY_EXISTS");
    }

    [Fact]
    public void DuplicateMediaPlatformException_ShouldHaveCorrectErrorCode()
    {
        var ex = new DuplicateMediaPlatformException(Guid.NewGuid(), Guid.NewGuid());

        ex.Should().BeAssignableTo<ConflictException>();
        ex.Code.Should().Be("DUPLICATE_MEDIA_PLATFORM");
    }

    [Fact]
    public void DuplicateEpisodePlatformException_ShouldHaveCorrectErrorCode()
    {
        var ex = new DuplicateEpisodePlatformException(Guid.NewGuid(), Guid.NewGuid());

        ex.Should().BeAssignableTo<ConflictException>();
        ex.Code.Should().Be("DUPLICATE_EPISODE_PLATFORM");
    }

    [Fact]
    public void DuplicateMediaMoodException_ShouldHaveCorrectErrorCode()
    {
        var ex = new DuplicateMediaMoodException(Guid.NewGuid(), Guid.NewGuid());

        ex.Should().BeAssignableTo<ConflictException>();
        ex.Code.Should().Be("DUPLICATE_MEDIA_MOOD");
    }

    [Fact]
    public void GenreNotFoundException_ShouldHaveCorrectErrorCode()
    {
        var id = Guid.NewGuid();
        var ex = new GenreNotFoundException(id);

        ex.Should().BeAssignableTo<NotFoundException>();
        ex.Code.Should().Be("GENRE_NOT_FOUND");
        ex.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void GenreCodeAlreadyExistsException_ShouldHaveCorrectErrorCode()
    {
        var ex = new GenreCodeAlreadyExistsException("action");

        ex.Should().BeAssignableTo<ConflictException>();
        ex.Code.Should().Be("GENRE_CODE_ALREADY_EXISTS");
    }

    [Fact]
    public void DuplicateMediaGenreException_ShouldHaveCorrectErrorCode()
    {
        var ex = new DuplicateMediaGenreException(Guid.NewGuid(), Guid.NewGuid());

        ex.Should().BeAssignableTo<ConflictException>();
        ex.Code.Should().Be("DUPLICATE_MEDIA_GENRE");
    }

    [Fact]
    public void DuplicateImdbIdException_ShouldHaveCorrectErrorCode()
    {
        var ex = new DuplicateImdbIdException("tt1375666");

        ex.Should().BeAssignableTo<ConflictException>();
        ex.Code.Should().Be("DUPLICATE_IMDB_ID");
        ex.Message.Should().Contain("tt1375666");
    }

    [Fact]
    public void DuplicateEpisodeException_ShouldHaveCorrectErrorCode()
    {
        var ex = new DuplicateEpisodeException(1, 2);

        ex.Should().BeAssignableTo<ConflictException>();
        ex.Code.Should().Be("DUPLICATE_EPISODE");
    }

    [Fact]
    public void DuplicateTmdbIdException_ShouldHaveCorrectErrorCode()
    {
        var ex = new DuplicateTmdbIdException(550);

        ex.Should().BeAssignableTo<ConflictException>();
        ex.Code.Should().Be("DUPLICATE_TMDB_ID");
        ex.Message.Should().Contain("550");
    }
}
