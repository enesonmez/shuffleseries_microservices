using System.Reflection;
using ShuffleSeries.ArchitectureTests.Common;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Infrastructure.Persistence;

namespace ShuffleSeries.ArchitectureTests;

/// <summary>
/// Catalog Bounded Context mimari kurallarını doğrular.
/// Ortak Onion, CQRS, DDD ve altyapı kurallarını <see cref="MicroserviceArchitectureTestBase"/> üzerinden miras alır.
/// </summary>
public class CatalogArchitectureTests : MicroserviceArchitectureTestBase
{
    protected override Assembly DomainAssembly => typeof(Series).Assembly;
    protected override Assembly ApplicationAssembly => typeof(CreateSeriesCommand).Assembly;
    protected override Assembly InfrastructureAssembly => typeof(CatalogDbContext).Assembly;
    protected override Assembly ApiAssembly => typeof(ShuffleSeries.Catalog.Api.Endpoints.SeriesEndpoints).Assembly;
    protected override string ServiceNamespacePrefix => "ShuffleSeries.Catalog";

    protected override IEnumerable<string> ForbiddenBoundedContextPrefixes => ["ShuffleSeries.Identity"];

    protected override IEnumerable<Type> AggregateRootTypes =>
    [
        typeof(Media),
        typeof(Series),
        typeof(Movie),
        typeof(Platform),
        typeof(Mood),
        typeof(Genre)
    ];

    protected override IEnumerable<Type> ChildEntityTypes =>
    [
        typeof(Episode),
        typeof(Season),
        typeof(EpisodePlatform),
        typeof(EpisodeTranslation),
        typeof(MediaPlatform),
        typeof(MediaMood),
        typeof(MediaGenre),
        typeof(MediaTranslation),
        typeof(MoodTranslation),
        typeof(GenreTranslation)
    ];

    #region Catalog-Specific Rules

    [Fact]
    public void CacheServices_Implementing_ICatalogCacheService_ShouldBeSealed()
    {
        var cacheServiceTypes = InfrastructureAssembly.GetTypes()
            .Where(t => typeof(ShuffleSeries.Catalog.Application.Common.Interfaces.ICatalogCacheService).IsAssignableFrom(t)
                        && t is { IsClass: true, IsAbstract: false })
            .ToList();

        cacheServiceTypes.Should().NotBeEmpty("Altyapıda ICatalogCacheService uygulayan en az bir servis bulunmalıdır.");

        foreach (var service in cacheServiceTypes)
        {
            service.IsSealed.Should().BeTrue(
                $"ICatalogCacheService uygulayan '{service.Name}' sınıfı performans ve güvenlik için 'sealed' olmalıdır.");
        }
    }

    [Fact]
    public void GrpcServices_ShouldBeSealed_And_NotDirectlyDependOn_RepositoriesOrDbContext()
    {
        var grpcServices = ApiAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsNested: false }
                        && !t.Name.Contains('<')
                        && t.Namespace == "ShuffleSeries.Catalog.Api.Grpc"
                        && t.Name.EndsWith("GrpcService", StringComparison.Ordinal))
            .ToList();

        grpcServices.Should().NotBeEmpty("Catalog.Api.Grpc katmanında en az bir gRPC servis sınıfı bulunmalıdır.");

        foreach (var service in grpcServices)
        {
            service.IsPublic.Should().BeTrue($"gRPC servisi '{service.Name}' public olmalıdır.");
            service.IsSealed.Should().BeTrue($"gRPC servisi '{service.Name}' sealed olmalıdır.");
        }

        var handlerTypeNames = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsNested: false }
                        && !t.Name.Contains('<')
                        && t.Namespace != null
                        && t.Namespace.Contains(".Features.")
                        && t.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Select(t => t.FullName!)
            .ToArray();

        var forbiddenDependencies = new[]
        {
            "ShuffleSeries.Catalog.Domain.Repositories",
            "ShuffleSeries.Catalog.Infrastructure.Persistence"
        }.Concat(handlerTypeNames).ToArray();

        var testResult = NetArchTest.Rules.Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("ShuffleSeries.Catalog.Api.Grpc")
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDependencies)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"gRPC servisleri doğrudan Repository, DbContext veya CQRS Handler kullanamaz; Wolverine IMessageBus üzerinden ilerlemelidir. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    #endregion
}
