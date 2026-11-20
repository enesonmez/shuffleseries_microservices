namespace ShuffleSeries.Catalog.Application.Features.Movies.Responses;

public sealed record MovieMoodDto(
    Guid MoodId,
    string Code,
    string Name,
    string AmbientColorHex,
    string? IconUrl);
