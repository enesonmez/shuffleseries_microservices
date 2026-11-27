namespace ShuffleSeries.ShuffleEngine.Domain.Models;

public sealed record ShuffleEpisodeSnapshot(
    Guid EpisodeId,
    Guid SeriesId,
    Guid SeasonId,
    int SeasonNumber,
    int EpisodeNumber,
    int? RuntimeMinutes,
    double? ImdbRating,
    string? ThumbnailUrl,
    string SeriesPosterUrl,
    int ReleaseYear,
    double BaseQualityScore,
    bool IsSafetyNetEligible,
    IReadOnlyList<Guid> PlatformIds,
    IReadOnlyList<Guid> MoodIds,
    IReadOnlyList<Guid> GenreIds,
    IReadOnlyList<ShufflePlatformDeepLink> PlatformLinks,
    IReadOnlyDictionary<string, ShuffleLocalizedText> Translations,
    IReadOnlyDictionary<string, ShuffleLocalizedText> SeriesTranslations);
