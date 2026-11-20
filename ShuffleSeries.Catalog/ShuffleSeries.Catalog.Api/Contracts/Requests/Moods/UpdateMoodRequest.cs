namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Moods;

public sealed record UpdateMoodRequest(
    string Code,
    string AmbientColorHex,
    string? IconUrl = null,
    int DisplayOrder = 0);
