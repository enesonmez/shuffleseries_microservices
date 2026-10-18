using ShuffleSeries.Catalog.Domain.Enums;

namespace ShuffleSeries.Catalog.Application.Features.Series.Responses;

public sealed record SeriesResponse(
    Guid Id,
    int ReleaseYear,
    SeriesType SeriesType,
    int TotalSeasons,
    string PosterUrl,
    string? BackdropUrl,
    string? ImdbId,
    double? ImdbRating,
    int? ImdbVotes,
    IReadOnlyList<SeriesTranslationResponse> Translations);

public sealed record SeriesTranslationResponse(
    string LanguageCode,
    string Title,
    string Overview,
    string? Tagline);
