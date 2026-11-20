namespace ShuffleSeries.Catalog.Application.Features.Episodes.Responses;

public sealed record EpisodePlatformDto(
    Guid PlatformId,
    string Name,
    string Slug,
    string LogoUrl,
    string WebUrl,
    string? AppSchemeUrl);
