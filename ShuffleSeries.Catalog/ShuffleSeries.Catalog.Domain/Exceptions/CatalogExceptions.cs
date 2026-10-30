using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class MediaNotFoundException : NotFoundException
{
    public MediaNotFoundException(Guid mediaId)
        : base($"Media with id '{mediaId}' was not found.", "MEDIA_NOT_FOUND")
    {
    }
}

public sealed class MovieNotFoundException : NotFoundException
{
    public MovieNotFoundException(Guid movieId)
        : base($"Movie with id '{movieId}' was not found.", "MOVIE_NOT_FOUND")
    {
    }
}

public sealed class SeriesNotFoundException : NotFoundException
{
    public SeriesNotFoundException(Guid seriesId)
        : base($"Series with id '{seriesId}' was not found.", "SERIES_NOT_FOUND")
    {
    }
}

public sealed class SeasonNotFoundException : NotFoundException
{
    public SeasonNotFoundException(Guid seasonId)
        : base($"Season with id '{seasonId}' was not found.", "SEASON_NOT_FOUND")
    {
    }
}

public sealed class EpisodeNotFoundException : NotFoundException
{
    public EpisodeNotFoundException(Guid episodeId)
        : base($"Episode with id '{episodeId}' was not found.", "EPISODE_NOT_FOUND")
    {
    }
}

public sealed class PlatformNotFoundException : NotFoundException
{
    public PlatformNotFoundException(Guid platformId)
        : base($"Platform with id '{platformId}' was not found.", "PLATFORM_NOT_FOUND")
    {
    }
}

public sealed class PlatformSlugAlreadyExistsException : ConflictException
{
    public PlatformSlugAlreadyExistsException(string slug)
        : base($"Platform with slug '{slug}' already exists.", "PLATFORM_SLUG_ALREADY_EXISTS")
    {
    }
}

public sealed class MoodNotFoundException : NotFoundException
{
    public MoodNotFoundException(Guid moodId)
        : base($"Mood with id '{moodId}' was not found.", "MOOD_NOT_FOUND")
    {
    }
}

public sealed class MoodCodeAlreadyExistsException : ConflictException
{
    public MoodCodeAlreadyExistsException(string code)
        : base($"Mood with code '{code}' already exists.", "MOOD_CODE_ALREADY_EXISTS")
    {
    }
}

public sealed class InvalidLanguageCodeException : BadRequestException
{
    public InvalidLanguageCodeException(string languageCode)
        : base($"Language code '{languageCode}' is invalid. Supported codes are ISO 639-1 format (e.g. 'tr', 'en').", "INVALID_LANGUAGE_CODE")
    {
    }
}

public sealed class DuplicateMediaPlatformException : ConflictException
{
    public DuplicateMediaPlatformException(Guid mediaId, Guid platformId)
        : base($"Media '{mediaId}' is already associated with platform '{platformId}'.", "DUPLICATE_MEDIA_PLATFORM")
    {
    }
}

public sealed class DuplicateEpisodePlatformException : ConflictException
{
    public DuplicateEpisodePlatformException(Guid episodeId, Guid platformId)
        : base($"Episode '{episodeId}' is already associated with platform '{platformId}'.", "DUPLICATE_EPISODE_PLATFORM")
    {
    }
}

public sealed class DuplicateMediaMoodException : ConflictException
{
    public DuplicateMediaMoodException(Guid mediaId, Guid moodId)
        : base($"Media '{mediaId}' is already associated with mood '{moodId}'.", "DUPLICATE_MEDIA_MOOD")
    {
    }
}

public sealed class GenreNotFoundException : NotFoundException
{
    public GenreNotFoundException(Guid genreId)
        : base($"Genre with id '{genreId}' was not found.", "GENRE_NOT_FOUND")
    {
    }
}

public sealed class GenreCodeAlreadyExistsException : ConflictException
{
    public GenreCodeAlreadyExistsException(string code)
        : base($"Genre with code '{code}' already exists.", "GENRE_CODE_ALREADY_EXISTS")
    {
    }
}

public sealed class DuplicateMediaGenreException : ConflictException
{
    public DuplicateMediaGenreException(Guid mediaId, Guid genreId)
        : base($"Media '{mediaId}' is already associated with genre '{genreId}'.", "DUPLICATE_MEDIA_GENRE")
    {
    }
}

public sealed class DuplicateImdbIdException : ConflictException
{
    public DuplicateImdbIdException(string imdbId)
        : base($"Media with IMDb ID '{imdbId}' already exists.", "DUPLICATE_IMDB_ID")
    {
    }
}

public sealed class DuplicateTmdbIdException : ConflictException
{
    public DuplicateTmdbIdException(int tmdbId)
        : base($"Media with TMDb ID '{tmdbId}' already exists.", "DUPLICATE_TMDB_ID")
    {
    }
}

public sealed class DuplicateEpisodeException : ConflictException
{
    public DuplicateEpisodeException(int seasonNumber, int episodeNumber)
        : base($"Episode {episodeNumber} already exists in Season {seasonNumber}.", "DUPLICATE_EPISODE")
    {
    }
}
