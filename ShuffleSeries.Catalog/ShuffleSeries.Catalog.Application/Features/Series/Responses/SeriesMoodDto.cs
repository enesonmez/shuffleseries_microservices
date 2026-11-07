namespace ShuffleSeries.Catalog.Application.Features.Series.Responses;

public sealed record SeriesMoodDto(
    Guid MoodId,
    string Code,
    string Name,
    string AmbientColorHex,
    string? IconUrl);
