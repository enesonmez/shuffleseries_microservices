namespace ShuffleSeries.Catalog.Application.Features.Series.Responses;

public sealed record SeriesSeasonDto(
    Guid SeasonId,
    int SeasonNumber,
    string? Title,
    IReadOnlyList<SeriesEpisodeDto> Episodes);
