namespace ShuffleSeries.Catalog.Application.Features.Series.Responses;

public sealed record SeriesTranslationDto(
    string LanguageCode,
    string Title,
    string Overview,
    string? Tagline);
