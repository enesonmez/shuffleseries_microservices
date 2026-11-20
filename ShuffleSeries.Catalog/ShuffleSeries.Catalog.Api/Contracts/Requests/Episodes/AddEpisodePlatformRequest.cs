namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Episodes;

public sealed record AddEpisodePlatformRequest(
    Guid PlatformId,
    string WebUrl,
    string? AppSchemeUrl = null);
