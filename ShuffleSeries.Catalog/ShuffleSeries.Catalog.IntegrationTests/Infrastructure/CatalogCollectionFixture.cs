namespace ShuffleSeries.Catalog.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public class CatalogCollectionFixture : ICollectionFixture<CatalogApiFactory>
{
    public const string Name = "CatalogIntegrationTests";
}
