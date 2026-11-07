namespace ShuffleSeries.Catalog.Api.Contracts.Requests.MediaAssociations;

public sealed record AddOrUpdateTranslationRequest(
    string LanguageCode,
    string Title,
    string Overview,
    string? Tagline = null);
