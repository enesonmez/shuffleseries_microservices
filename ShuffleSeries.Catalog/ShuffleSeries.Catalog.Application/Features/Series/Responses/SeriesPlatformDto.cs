namespace ShuffleSeries.Catalog.Application.Features.Series.Responses;

public sealed record SeriesPlatformDto(
    Guid PlatformId,
    string Name,
    string Slug,
    string LogoUrl,
    string WebUrl,
    string? AppSchemeUrl);
