namespace ShuffleSeries.Catalog.Application.Features.Movies.Responses;

public sealed record MovieTranslationDto(
    string LanguageCode,
    string Title,
    string Overview,
    string? Tagline);
