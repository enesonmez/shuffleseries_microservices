namespace ShuffleSeries.Catalog.Application.Features.Platforms.Responses;

public sealed record PlatformResponse(
    Guid Id,
    string Name,
    string Slug,
    string LogoUrl,
    string InactiveLogoUrl,
    string BaseWebUrl,
    string? BaseAppSchemeUrl,
    int DisplayOrder,
    bool IsActive);
