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
    public const int MaxPageNumber = 10_000;

    private readonly int _pageNumber = DefaultPageNumber;
    private readonly int _pageSize = DefaultPageSize;

    /// <summary>
    /// Sayfa numarası (1 ile 10_000 arasında).
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? DefaultPageNumber : Math.Min(value, MaxPageNumber);
    }

    /// <summary>
    /// Sayfa başına kayıt adedi (1 ile 100 arasında).
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize);
    }

    /// <summary>
    /// "PageNumber" için kolaylık sağlayan takma ad (alias).
    /// </summary>
    public int Page => PageNumber;

    /// <summary>
    /// SQL sorgularında atlanacak kayıt sayısı (OFFSET).
    /// Asla negatif olamaz ve overflow yapamaz.
    /// </summary>
    public int Skip => Math.Max(0, (PageNumber - 1) * PageSize);

    /// <summary>
    /// SQL sorgularında alınacak kayıt sayısı (LIMIT).
    /// </summary>
    public int Take => PageSize;

    public PaginationRequest(int? pageNumber, int? pageSize)
    {
        PageNumber = pageNumber ?? DefaultPageNumber;
        PageSize = pageSize ?? DefaultPageSize;
    }

    public PaginationRequest() : this(DefaultPageNumber, DefaultPageSize)
    {
    }

    public static PaginationRequest Create(int? pageNumber, int? pageSize) => new(pageNumber, pageSize);
}
