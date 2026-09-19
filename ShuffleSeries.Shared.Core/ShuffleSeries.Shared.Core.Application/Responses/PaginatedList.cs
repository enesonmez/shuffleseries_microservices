using System.Text.Json.Serialization;

namespace ShuffleSeries.Shared.Core.Application.Responses;

public class PaginatedList<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    [JsonConstructor]
    public PaginatedList(IReadOnlyCollection<T> items, int totalCount, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
        TotalCount = totalCount;
        Items = items;
    }

    public static PaginatedList<T> Create(IReadOnlyCollection<T> items, int totalCount, int pageNumber, int pageSize)
        => new(items, totalCount, pageNumber, pageSize);

    public static PaginatedList<T> Empty(int pageNumber = 1, int pageSize = 10)
        => new([], 0, pageNumber, pageSize);
}