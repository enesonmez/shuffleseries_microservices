namespace ShuffleSeries.ShuffleEngine.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public class ShuffleEngineCollectionFixture : ICollectionFixture<ShuffleEngineApiFactory>
{
    public const string Name = "ShuffleEngineIntegrationCollection";
}
