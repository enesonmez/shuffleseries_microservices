namespace ShuffleSeries.Catalog.Application.Features.Movies.Responses;

public sealed record MoviePlatformDto(
    Guid PlatformId,
    string Name,
    string Slug,
    string LogoUrl,
    string WebUrl,
    string? AppSchemeUrl);
