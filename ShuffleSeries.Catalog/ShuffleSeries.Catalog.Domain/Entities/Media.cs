using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Events;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Domain.Entities;

public abstract class Media : AggregateRoot
{
    private readonly List<MediaTranslation> _translations = [];
    private readonly List<MediaPlatform> _mediaPlatforms = [];
    private readonly List<MediaMood> _mediaMoods = [];
    private readonly List<MediaGenre> _mediaGenres = [];

    public MediaType MediaType { get; protected set; }
    public string? ImdbId { get; protected set; }
    public double? ImdbRating { get; protected set; }
    public int? ImdbVotes { get; protected set; }
    public int ReleaseYear { get; protected set; }
    public string PosterUrl { get; protected set; } = string.Empty;
    public string? BackdropUrl { get; protected set; }

    public IReadOnlyCollection<MediaTranslation> Translations => _translations.AsReadOnly();
    public IReadOnlyCollection<MediaPlatform> MediaPlatforms => _mediaPlatforms.AsReadOnly();
    public IReadOnlyCollection<MediaMood> MediaMoods => _mediaMoods.AsReadOnly();
    public IReadOnlyCollection<MediaGenre> MediaGenres => _mediaGenres.AsReadOnly();

    protected Media() : base(Guid.NewGuid()) { }

    protected Media(
        Guid id,
        MediaType mediaType,
        string? imdbId,
        double? imdbRating,
        int? imdbVotes,
        int releaseYear,
        string posterUrl,
        string? backdropUrl) : base(id)
    {
        if (string.IsNullOrWhiteSpace(posterUrl))
            throw new ArgumentException("Poster URL cannot be empty.", nameof(posterUrl));

        ValidateReleaseYear(releaseYear);
        ValidateRatings(imdbRating, imdbVotes);

        MediaType = mediaType;
        ImdbId = string.IsNullOrWhiteSpace(imdbId) ? null : imdbId.Trim();
        ImdbRating = imdbRating;
        ImdbVotes = imdbVotes;
        ReleaseYear = releaseYear;
        PosterUrl = posterUrl.Trim();
        BackdropUrl = string.IsNullOrWhiteSpace(backdropUrl) ? null : backdropUrl.Trim();
    }

    public MediaTranslation AddOrUpdateTranslation(
        LanguageCode languageCode,
        string title,
        string overview,
        string? tagline = null)
    {
        ArgumentNullException.ThrowIfNull(languageCode);

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        if (string.IsNullOrWhiteSpace(overview))
            throw new ArgumentException("Overview cannot be empty.", nameof(overview));

        var existing = _translations.FirstOrDefault(t => t.LanguageCode == languageCode);

        if (existing is not null)
        {
            existing.Update(title, overview, tagline);
            ModifiedAtUtc = DateTime.UtcNow;
            RaiseUpdatedEvent();
            return existing;
        }

        var translation = new MediaTranslation(Id, languageCode, title, overview, tagline);
        _translations.Add(translation);
        ModifiedAtUtc = DateTime.UtcNow;
        RaiseUpdatedEvent();

        return translation;
    }

    public MediaTranslation AddOrUpdateTranslation(
        string languageCode,
        string title,
        string overview,
        string? tagline = null)
    {
        return AddOrUpdateTranslation(LanguageCode.From(languageCode), title, overview, tagline);
    }

    public void RemoveTranslation(LanguageCode languageCode)
    {
        if (languageCode is null)
            return;

        foreach (var item in _translations.Where(t => t.LanguageCode == languageCode))
        {
            item.HardDelete();
        }

        var removed = _translations.RemoveAll(t => t.LanguageCode == languageCode);
        if (removed > 0)
        {
            ModifiedAtUtc = DateTime.UtcNow;
            RaiseUpdatedEvent();
        }
    }

    public void RemoveTranslation(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return;

        if (LanguageCode.TryFrom(languageCode, out var parsed))
            RemoveTranslation(parsed);
    }

    public MediaPlatform AddPlatform(Guid platformId, string webUrl, string? appSchemeUrl = null)
    {
        if (_mediaPlatforms.Any(p => p.PlatformId == platformId))
            throw new DuplicateMediaPlatformException(Id, platformId);

        var mediaPlatform = new MediaPlatform(Id, platformId, webUrl, appSchemeUrl);
        _mediaPlatforms.Add(mediaPlatform);
        ModifiedAtUtc = DateTime.UtcNow;
        RaiseUpdatedEvent();

        return mediaPlatform;
    }

    public void UpdatePlatformUrls(Guid platformId, string webUrl, string? appSchemeUrl = null)
    {
        var existing = _mediaPlatforms.FirstOrDefault(p => p.PlatformId == platformId)
            ?? throw new PlatformNotFoundException(platformId);

        existing.UpdateUrls(webUrl, appSchemeUrl);
        ModifiedAtUtc = DateTime.UtcNow;
        RaiseUpdatedEvent();
    }

    public void RemovePlatform(Guid platformId)
    {
        foreach (var item in _mediaPlatforms.Where(p => p.PlatformId == platformId))
        {
            item.HardDelete();
        }

        var removed = _mediaPlatforms.RemoveAll(p => p.PlatformId == platformId);
        if (removed > 0)
        {
            ModifiedAtUtc = DateTime.UtcNow;
            RaiseUpdatedEvent();
        }
    }

    public MediaMood AddMood(Guid moodId)
    {
        if (_mediaMoods.Any(m => m.MoodId == moodId))
            throw new DuplicateMediaMoodException(Id, moodId);

        var mediaMood = new MediaMood(Id, moodId);
        _mediaMoods.Add(mediaMood);
        ModifiedAtUtc = DateTime.UtcNow;
        RaiseUpdatedEvent();

        return mediaMood;
    }

    public void RemoveMood(Guid moodId)
    {
        foreach (var item in _mediaMoods.Where(m => m.MoodId == moodId))
        {
            item.HardDelete();
        }

        var removed = _mediaMoods.RemoveAll(m => m.MoodId == moodId);
        if (removed > 0)
        {
            ModifiedAtUtc = DateTime.UtcNow;
            RaiseUpdatedEvent();
        }
    }

    public MediaGenre AddGenre(Guid genreId)
    {
        if (_mediaGenres.Any(g => g.GenreId == genreId))
            throw new DuplicateMediaGenreException(Id, genreId);

        var mediaGenre = new MediaGenre(Id, genreId);
        _mediaGenres.Add(mediaGenre);
        ModifiedAtUtc = DateTime.UtcNow;
        RaiseUpdatedEvent();

        return mediaGenre;
    }

    public void RemoveGenre(Guid genreId)
    {
        foreach (var item in _mediaGenres.Where(g => g.GenreId == genreId))
        {
            item.HardDelete();
        }

        var removed = _mediaGenres.RemoveAll(g => g.GenreId == genreId);
        if (removed > 0)
        {
            ModifiedAtUtc = DateTime.UtcNow;
            RaiseUpdatedEvent();
        }
    }

    public void UpdateVisuals(string posterUrl, string? backdropUrl = null)
    {
        if (string.IsNullOrWhiteSpace(posterUrl))
            throw new ArgumentException("Poster URL cannot be empty.", nameof(posterUrl));

        PosterUrl = posterUrl.Trim();
        BackdropUrl = string.IsNullOrWhiteSpace(backdropUrl) ? null : backdropUrl.Trim();
        ModifiedAtUtc = DateTime.UtcNow;

        RaiseUpdatedEvent();
    }

    public void UpdateRatings(string? imdbId, double? imdbRating, int? imdbVotes, int releaseYear)
    {
        ValidateReleaseYear(releaseYear);
        ValidateRatings(imdbRating, imdbVotes);

        ImdbId = string.IsNullOrWhiteSpace(imdbId) ? null : imdbId.Trim();
        ImdbRating = imdbRating;
        ImdbVotes = imdbVotes;
        ReleaseYear = releaseYear;
        ModifiedAtUtc = DateTime.UtcNow;

        RaiseUpdatedEvent();
    }

    public virtual void Delete(string? deletedBy = null)
    {
        SoftDelete(deletedBy);
        RaiseDomainEvent(new MediaDeletedDomainEvent(Id, MediaType));
    }

    protected static void ValidateReleaseYear(int releaseYear)
    {
        if (releaseYear is < 1888 or > 2100)
            throw new ArgumentOutOfRangeException(nameof(releaseYear), "Release year must be between 1888 and 2100.");
    }

    protected static void ValidateRatings(double? imdbRating, int? imdbVotes)
    {
        if (imdbRating is < 0.0 or > 10.0)
            throw new ArgumentOutOfRangeException(nameof(imdbRating), "IMDb rating must be between 0.0 and 10.0.");

        if (imdbVotes is < 0)
            throw new ArgumentOutOfRangeException(nameof(imdbVotes), "IMDb votes cannot be negative.");
    }

    protected void RaiseUpdatedEvent()
    {
        if (GetDomainEvents().Any(e => e is MediaCreatedDomainEvent or MediaUpdatedDomainEvent))
            return;

        RaiseDomainEvent(new MediaUpdatedDomainEvent(Id, MediaType));
    }
}
