namespace ShuffleSeries.Catalog.Application.Common.Interfaces;

public interface ICatalogCacheService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    void Remove(string key);
}
