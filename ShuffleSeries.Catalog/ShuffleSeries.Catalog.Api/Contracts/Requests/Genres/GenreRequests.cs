namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Genres;

public sealed record CreateGenreRequest(
    string Code,
    string Name,
    string LanguageCode,
    string? Description = null,
    int DisplayOrder = 0,
    bool IsActive = true);

public sealed record UpdateGenreRequest(
    string Code,
    int DisplayOrder = 0);

public sealed record AddOrUpdateGenreTranslationRequest(
    string LanguageCode,
    string Name,
    string? Description = null);
