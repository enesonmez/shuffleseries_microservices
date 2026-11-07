namespace ShuffleSeries.Catalog.Application.Features.Genres.Responses;

public sealed record GenreTranslationResponse(
    string LanguageCode,
    string Name,
    string? Description);
