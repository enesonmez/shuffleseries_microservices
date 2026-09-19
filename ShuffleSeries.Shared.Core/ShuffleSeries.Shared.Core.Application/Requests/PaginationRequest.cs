namespace ShuffleSeries.Shared.Core.Application.Requests;

public record PaginationRequest
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    public int PageNumber { get; init; }
    public int PageSize { get; init; }

    public PaginationRequest(int pageNumber = DefaultPageNumber, int pageSize = DefaultPageSize)
    {
        PageNumber = pageNumber < 1 ? DefaultPageNumber : pageNumber;
        PageSize = pageSize < 1 ? DefaultPageSize : (pageSize > MaxPageSize ? MaxPageSize : pageSize);
    }

    public PaginationRequest() : this(DefaultPageNumber, DefaultPageSize)
    {
    }
}
