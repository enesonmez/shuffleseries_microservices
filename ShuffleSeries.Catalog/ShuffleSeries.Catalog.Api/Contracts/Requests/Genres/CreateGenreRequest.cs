namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Genres;

public sealed record CreateGenreRequest(
    string Code,
    string Name,
    string LanguageCode,
    string? Description = null,
    int DisplayOrder = 0,
    bool IsActive = true);
