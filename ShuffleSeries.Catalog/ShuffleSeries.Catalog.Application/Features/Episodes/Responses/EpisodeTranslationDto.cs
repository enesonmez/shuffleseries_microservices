namespace ShuffleSeries.Catalog.Application.Features.Episodes.Responses;

public sealed record EpisodeTranslationDto(
    string LanguageCode,
    string Title,
    string? Overview);
