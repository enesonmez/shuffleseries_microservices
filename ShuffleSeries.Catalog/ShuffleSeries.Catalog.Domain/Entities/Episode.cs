using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class Episode : BaseEntity
{
    private readonly List<EpisodeTranslation> _translations = [];
    private readonly List<EpisodePlatform> _episodePlatforms = [];

    public Guid SeasonId { get; private set; }
    public int EpisodeNumber { get; private set; }
    public bool IsStandalone { get; private set; }
    public int? RuntimeMinutes { get; private set; }
    public double? ImdbRating { get; private set; }
    public string? ThumbnailUrl { get; private set; }

    public IReadOnlyCollection<EpisodeTranslation> Translations => _translations.AsReadOnly();
    public IReadOnlyCollection<EpisodePlatform> EpisodePlatforms => _episodePlatforms.AsReadOnly();

    private Episode() : base(Guid.Empty) { }

    internal Episode(
        Guid seasonId,
        int episodeNumber,
        bool isStandalone,
        int? runtimeMinutes = null,
        double? imdbRating = null,
        string? thumbnailUrl = null) : base(Guid.Empty)
    {
        if (episodeNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(episodeNumber), "Episode number must be greater than zero.");

        if (runtimeMinutes is <= 0)
            throw new ArgumentOutOfRangeException(nameof(runtimeMinutes), "Runtime minutes must be greater than zero.");

        if (imdbRating is < 0.0 or > 10.0)
            throw new ArgumentOutOfRangeException(nameof(imdbRating), "IMDb rating must be between 0.0 and 10.0.");

        SeasonId = seasonId;
        EpisodeNumber = episodeNumber;
        IsStandalone = isStandalone;
        RuntimeMinutes = runtimeMinutes;
        ImdbRating = imdbRating;
        ThumbnailUrl = string.IsNullOrWhiteSpace(thumbnailUrl) ? null : thumbnailUrl.Trim();
    }

    public void UpdateDetails(
        int episodeNumber,
        bool isStandalone,
        int? runtimeMinutes = null,
        double? imdbRating = null,
        string? thumbnailUrl = null)
    {
        if (episodeNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(episodeNumber), "Episode number must be greater than zero.");

        if (runtimeMinutes is <= 0)
            throw new ArgumentOutOfRangeException(nameof(runtimeMinutes), "Runtime minutes must be greater than zero.");

        if (imdbRating is < 0.0 or > 10.0)
            throw new ArgumentOutOfRangeException(nameof(imdbRating), "IMDb rating must be between 0.0 and 10.0.");

        EpisodeNumber = episodeNumber;
        IsStandalone = isStandalone;
        RuntimeMinutes = runtimeMinutes;
        ImdbRating = imdbRating;
        ThumbnailUrl = string.IsNullOrWhiteSpace(thumbnailUrl) ? null : thumbnailUrl.Trim();
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public EpisodeTranslation AddOrUpdateTranslation(LanguageCode languageCode, string title, string? overview = null)
    {
        ArgumentNullException.ThrowIfNull(languageCode);

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        var existing = _translations.FirstOrDefault(t => t.LanguageCode == languageCode);

        if (existing is not null)
        {
            existing.Update(title, overview);
            ModifiedAtUtc = DateTime.UtcNow;
            return existing;
        }

        var translation = new EpisodeTranslation(Id, languageCode, title, overview);
        _translations.Add(translation);
        ModifiedAtUtc = DateTime.UtcNow;

        return translation;
    }

    public EpisodeTranslation AddOrUpdateTranslation(string languageCode, string title, string? overview = null)
    {
        return AddOrUpdateTranslation(LanguageCode.From(languageCode), title, overview);
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
        }
    }

    public void RemoveTranslation(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return;

        if (LanguageCode.TryFrom(languageCode, out var parsed))
            RemoveTranslation(parsed);
    }

    public EpisodePlatform AddPlatform(Guid platformId, string webUrl, string? appSchemeUrl = null)
    {
        var existing = _episodePlatforms.FirstOrDefault(p => p.PlatformId == platformId);
        if (existing is not null)
        {
            existing.UpdateUrls(webUrl, appSchemeUrl);
            ModifiedAtUtc = DateTime.UtcNow;
            return existing;
        }

        var episodePlatform = new EpisodePlatform(Id, platformId, webUrl, appSchemeUrl);
        _episodePlatforms.Add(episodePlatform);
        ModifiedAtUtc = DateTime.UtcNow;

        return episodePlatform;
    }

    public void RemovePlatform(Guid platformId)
    {
        foreach (var item in _episodePlatforms.Where(p => p.PlatformId == platformId))
        {
            item.HardDelete();
        }

        var removed = _episodePlatforms.RemoveAll(p => p.PlatformId == platformId);
        if (removed > 0)
        {
            ModifiedAtUtc = DateTime.UtcNow;
        }
    }

    public void SetStandalone(bool isStandalone)
    {
        IsStandalone = isStandalone;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}
