using System.Reflection;
using ShuffleSeries.ArchitectureTests.Common;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Domain.Models;
using ShuffleSeries.ShuffleEngine.Infrastructure.Persistence;

namespace ShuffleSeries.ArchitectureTests;

/// <summary>
/// ShuffleEngine Bounded Context mimari kurallarını doğrular.
/// Ortak Onion, CQRS, DDD ve altyapı kurallarını <see cref="MicroserviceArchitectureTestBase"/> üzerinden miras alır.
/// </summary>
public class ShuffleEngineArchitectureTests : MicroserviceArchitectureTestBase
{
    protected override Assembly DomainAssembly => typeof(ShuffleMediaSnapshot).Assembly;
    protected override Assembly ApplicationAssembly => typeof(ShuffleSeries.ShuffleEngine.Application.DependencyInjection).Assembly;
    protected override Assembly InfrastructureAssembly => typeof(ShuffleEngineDbContext).Assembly;
    protected override Assembly ApiAssembly => typeof(ShuffleSeries.ShuffleEngine.Api.Endpoints.ShuffleAdminEndpoints).Assembly;
    protected override string ServiceNamespacePrefix => "ShuffleSeries.ShuffleEngine";

    protected override IEnumerable<string> ForbiddenBoundedContextPrefixes =>
    [
        "ShuffleSeries.Catalog",
        "ShuffleSeries.Identity"
    ];

    protected override IEnumerable<Type> AggregateRootTypes => [];
    protected override IEnumerable<Type> ChildEntityTypes => [];

    [Fact]
    public void InfrastructureServices_ShouldBeInternalSealed()
    {
        Type[] coreInterfaces =
        [
            typeof(IShufflePoolStore),
            typeof(IUserShuffleStateStore),
            typeof(ICatalogSyncGrpcClient),
            typeof(IShuffleCacheWarmingService),
            typeof(IInboxIdempotencyService)
        ];

        foreach (var contract in coreInterfaces)
        {
            var implementations = InfrastructureAssembly.GetTypes()
                .Where(t => contract.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
                .ToList();

            implementations.Should().NotBeEmpty(
                $"ShuffleEngine.Infrastructure altında '{contract.Name}' sözleşmesini uygulayan en az bir sınıf bulunmalıdır.");

            foreach (var impl in implementations)
            {
                impl.IsSealed.Should().BeTrue($"Altyapı servisi '{impl.Name}' performans ve güvenlik için 'sealed' olmalıdır.");
                impl.IsNotPublic.Should().BeTrue($"Altyapı servisi '{impl.Name}' enkapsülasyon gereği 'internal' olmalıdır.");
            }
        }
    }
}
