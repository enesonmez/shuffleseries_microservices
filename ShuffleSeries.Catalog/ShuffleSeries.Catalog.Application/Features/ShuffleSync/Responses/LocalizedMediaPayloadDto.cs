namespace ShuffleSeries.Catalog.Application.Features.ShuffleSync.Responses;

public sealed record LocalizedMediaPayloadDto(
    string Title,
    string Overview,
    string? Tagline);
