namespace ShuffleSeries.Catalog.Application.Features.ShuffleSync.Responses;

public sealed record ShufflePlatformLinkDto(
    Guid PlatformId,
    string PlatformName,
    string PlatformSlug,
    string LogoUrl,
    string WebUrl,
    string? AppSchemeUrl);
