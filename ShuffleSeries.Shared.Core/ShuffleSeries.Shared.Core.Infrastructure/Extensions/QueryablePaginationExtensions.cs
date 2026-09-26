using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Shared.Core.Application.Requests;
using ShuffleSeries.Shared.Core.Application.Responses;

namespace ShuffleSeries.Shared.Core.Infrastructure.Extensions;

/// <summary>
/// EF Core ve LINQ sorgularına güvenli sayfalama (Pagination) yetenekleri ekleyen extension metotlar.
/// Sayfa ve boyut sınırlarını merkezi PaginationRequest üzerinden uygular.
/// </summary>
public static class QueryablePaginationExtensions
{
    /// <summary>
    /// Sorguya güvenli Skip (OFFSET) ve Take (LIMIT) uygular.
    /// Negatif OFFSET veya sınır aşımı durumlarını merkezi olarak engeller.
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> source, PaginationRequest? pagination)
    {
        ArgumentNullException.ThrowIfNull(source);

        pagination ??= new PaginationRequest();
        return source.Skip(pagination.Skip).Take(pagination.Take);
    }

    /// <summary>
    /// Sayfa numarası ve boyutu ile sorguya güvenli Skip ve Take uygular.
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> source, int? pageNumber, int? pageSize)
    {
        return source.ApplyPagination(new PaginationRequest(pageNumber, pageSize));
    }

    /// <summary>
    /// IQueryable sorgusunu asenkron olarak sayfalar ve PaginatedList nesnesine dönüştürür.
    /// Toplam kayıt sayısını ve sayfalanmış elemanları tek akışta çeker.
    /// </summary>
    public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(
        this IQueryable<T> source,
        PaginationRequest? pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        pagination ??= new PaginationRequest();

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    /// <summary>
    /// Sayfa numarası ve boyutu parametreleri ile asenkron olarak sayfalanmış liste döner.
    /// </summary>
    public static Task<PaginatedList<T>> ToPaginatedListAsync<T>(
        this IQueryable<T> source,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        return source.ToPaginatedListAsync(new PaginationRequest(pageNumber, pageSize), cancellationToken);
    }
}
