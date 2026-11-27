namespace ShuffleSeries.ShuffleEngine.Application.Common.Models;

public sealed record CatalogPagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
