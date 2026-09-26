namespace ShuffleSeries.Shared.Core.Application.Requests;

/// <summary>
/// Sayfalama parametrelerini güvenli bir şekilde normalize eden ve SQL limit/offset
/// hesaplamalarını merkezi olarak yöneten temel sayfalama sınıfı.
/// </summary>
public record PaginationRequest
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Sayfa numarası (1'den başlar).
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Sayfa başına kayıt adedi (1 ile 100 arasında).
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// "PageNumber" için kolaylık sağlayan takma ad (alias).
    /// </summary>
    public int Page => PageNumber;

    /// <summary>
    /// SQL sorgularında atlanacak kayıt sayısı (OFFSET).
    /// Asla negatif olamaz.
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// SQL sorgularında alınacak kayıt sayısı (LIMIT).
    /// </summary>
    public int Take => PageSize;

    public PaginationRequest(int? pageNumber = DefaultPageNumber, int? pageSize = DefaultPageSize)
    {
        PageNumber = pageNumber is null or < 1 ? DefaultPageNumber : pageNumber.Value;
        PageSize = pageSize is null or < 1
            ? DefaultPageSize
            : (pageSize.Value > MaxPageSize ? MaxPageSize : pageSize.Value);
    }

    public PaginationRequest() : this(DefaultPageNumber, DefaultPageSize)
    {
    }

    public static PaginationRequest Create(int? pageNumber, int? pageSize) => new(pageNumber, pageSize);
}
