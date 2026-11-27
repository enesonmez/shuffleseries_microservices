using ShuffleSeries.ShuffleEngine.Domain.Enums;

namespace ShuffleSeries.ShuffleEngine.Domain.Models;

public sealed record ShuffleMediaSnapshot(
    Guid Id,
    ShuffleMediaKind Kind,
    int TmdbId,
    string ImdbId,
    double? ImdbRating,
    int? ImdbVotes,
    int ReleaseYear,
    string PosterUrl,
    string? BackdropUrl,
    int? RuntimeMinutes,
    string? Director,
    int? SeriesType,
    int? TotalSeasons,
    double BaseQualityScore,
    bool IsSafetyNetEligible,
    IReadOnlyList<Guid> PlatformIds,
    IReadOnlyList<Guid> MoodIds,
    IReadOnlyList<Guid> GenreIds,
    IReadOnlyList<ShufflePlatformDeepLink> PlatformLinks,
    IReadOnlyDictionary<string, ShuffleLocalizedText> Translations);
