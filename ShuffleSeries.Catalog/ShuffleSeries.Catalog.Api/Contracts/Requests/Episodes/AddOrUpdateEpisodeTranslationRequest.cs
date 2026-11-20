namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Episodes;

public sealed record AddOrUpdateEpisodeTranslationRequest(
    string LanguageCode,
    string Title,
    string? Overview = null);
