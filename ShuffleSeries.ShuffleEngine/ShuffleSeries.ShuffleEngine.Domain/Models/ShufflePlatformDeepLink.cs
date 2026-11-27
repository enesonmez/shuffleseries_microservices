namespace ShuffleSeries.ShuffleEngine.Domain.Models;

public sealed record ShufflePlatformDeepLink(
    Guid PlatformId,
    string PlatformName,
    string PlatformSlug,
    string LogoUrl,
    string WebUrl,
    string? AppSchemeUrl);
