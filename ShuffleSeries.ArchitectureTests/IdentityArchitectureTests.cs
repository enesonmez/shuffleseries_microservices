using System.Reflection;
using ShuffleSeries.ArchitectureTests.Common;
using ShuffleSeries.Identity.Domain.Entities;

namespace ShuffleSeries.ArchitectureTests;

/// <summary>
/// Identity Bounded Context mimari kurallarını doğrular.
/// Ortak Onion, CQRS, DDD ve altyapı kurallarını <see cref="MicroserviceArchitectureTestBase"/> üzerinden miras alır.
/// </summary>
public class IdentityArchitectureTests : MicroserviceArchitectureTestBase
{
    protected override Assembly DomainAssembly => typeof(User).Assembly;
    protected override Assembly ApplicationAssembly => typeof(ShuffleSeries.Identity.Application.DependencyInjection).Assembly;
    protected override Assembly InfrastructureAssembly => typeof(ShuffleSeries.Identity.Infrastructure.DependencyInjection).Assembly;
    protected override Assembly ApiAssembly => typeof(ShuffleSeries.Identity.Api.Endpoints.AuthEndpoints).Assembly;
    protected override string ServiceNamespacePrefix => "ShuffleSeries.Identity";

    protected override IEnumerable<string> ForbiddenBoundedContextPrefixes =>
    [
        "ShuffleSeries.Catalog",
        "ShuffleSeries.ShuffleEngine"
    ];

    protected override IEnumerable<string> CompositeKeyEntityExclusionNames =>
    [
        "UserRole",
        "UserPermission",
        "RolePermission"
    ];

    protected override IEnumerable<Type> AggregateRootTypes =>
    [
        typeof(User),
        typeof(Role),
        typeof(Permission)
    ];

    protected override IEnumerable<Type> ChildEntityTypes =>
    [
        typeof(UserRole),
        typeof(UserPermission),
        typeof(RefreshToken),
        typeof(UserLogin),
        typeof(RolePermission)
    ];
}
