namespace ShuffleSeries.Identity.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public class IdentityCollectionFixture : ICollectionFixture<IdentityApiFactory>
{
    public const string Name = "IdentityIntegrationTests";
}
