namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Platforms;

public sealed record UpdatePlatformRequest(
    string Name,
    string Slug,
    string LogoUrl,
    string InactiveLogoUrl,
    string BaseWebUrl,
    string? BaseAppSchemeUrl = null,
    int DisplayOrder = 0);
