namespace ShuffleSeries.Catalog.Application.Common.Constants;

public static class CatalogCacheKeys
{
    public const string Platforms = "catalog:metadata:platforms";
    public const string Moods = "catalog:metadata:moods";
    public const string Genres = "catalog:metadata:genres";

    public static readonly TimeSpan DefaultMetadataExpiration = TimeSpan.FromHours(1);
}
