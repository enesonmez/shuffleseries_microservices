namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Genres;

public sealed record AddOrUpdateGenreTranslationRequest(
    string LanguageCode,
    string Name,
    string? Description = null);
