namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Genres;

public sealed record UpdateGenreRequest(
    string Code,
    int DisplayOrder = 0);
