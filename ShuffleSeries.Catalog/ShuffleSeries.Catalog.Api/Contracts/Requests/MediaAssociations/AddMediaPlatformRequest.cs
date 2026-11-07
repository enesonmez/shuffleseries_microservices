namespace ShuffleSeries.Catalog.Api.Contracts.Requests.MediaAssociations;

public sealed record AddMediaPlatformRequest(
    Guid PlatformId,
    string WebUrl,
    string? AppSchemeUrl = null);
