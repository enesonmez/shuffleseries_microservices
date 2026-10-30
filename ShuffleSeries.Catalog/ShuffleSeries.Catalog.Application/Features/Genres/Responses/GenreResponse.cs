namespace ShuffleSeries.Catalog.Application.Features.Genres.Responses;

public sealed record GenreResponse(
    Guid Id,
    string Code,
    int DisplayOrder,
    bool IsActive,
    string Name,
    string? Description,
    IReadOnlyList<GenreTranslationResponse> Translations);

public sealed record GenreTranslationResponse(
    string LanguageCode,
    string Name,
    string? Description);
