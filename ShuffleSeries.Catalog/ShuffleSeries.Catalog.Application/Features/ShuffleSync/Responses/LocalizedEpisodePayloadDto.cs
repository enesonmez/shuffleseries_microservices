namespace ShuffleSeries.Catalog.Application.Features.ShuffleSync.Responses;

public sealed record LocalizedEpisodePayloadDto(
    string Title,
    string? Overview);
