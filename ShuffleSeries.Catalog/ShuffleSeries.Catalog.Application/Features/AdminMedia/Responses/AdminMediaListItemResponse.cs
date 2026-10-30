using ShuffleSeries.Catalog.Domain.Enums;

namespace ShuffleSeries.Catalog.Application.Features.AdminMedia.Responses;

public sealed record AdminMediaListItemResponse(
    Guid Id,
    MediaType MediaType,
    int TmdbId,
    string ImdbId,
    string Title,
    string Overview,
    int ReleaseYear,
    string PosterUrl,
    double? ImdbRating,
    int? ImdbVotes,
    IReadOnlyList<string> PlatformNames,
    IReadOnlyList<string> MoodCodes,
    IReadOnlyList<string> GenreCodes,
    DateTime CreatedAtUtc,
    bool IsDeleted);
