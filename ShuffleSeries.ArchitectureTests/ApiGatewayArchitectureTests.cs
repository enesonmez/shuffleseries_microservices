using System.Reflection;
using NetArchTest.Rules;
using ShuffleSeries.ApiGateway.Api.Middlewares;

namespace ShuffleSeries.ArchitectureTests;

/// <summary>
/// YARP API Gateway Edge katmanının mikroservislerden ve doğrudan veritabanı sürücülerinden
/// tamamen izole kalmasını doğrulayan mimari testler.
/// </summary>
public class ApiGatewayArchitectureTests
{
    private static readonly Assembly _gatewayAssembly = typeof(TokenRevocationMiddleware).Assembly;

    [Fact]
    public void ApiGateway_ShouldNotDependOn_MicroserviceAssemblies()
    {
        string[] forbiddenServices =
        [
            "ShuffleSeries.Catalog",
            "ShuffleSeries.Identity",
            "ShuffleSeries.ShuffleEngine"
        ];

        var result = Types.InAssembly(_gatewayAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenServices)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"API Gateway hiçbir alt mikroservis assembly'sine doğrudan bağımlı olamaz. İhlaller: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void ApiGateway_ShouldNotDependOn_DatabaseProviders()
    {
        string[] forbiddenDatabasePackages =
        [
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "MongoDB"
        ];

        var result = Types.InAssembly(_gatewayAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDatabasePackages)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"API Gateway doğrudan veritabanı sürücüsü veya ORM kullanamaz; yalnızca YARP ve Redis Edge bileşenlerini barındırabilir. İhlaller: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void ApiGateway_Middlewares_ShouldBeSealed()
    {
        var middlewareTypes = _gatewayAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                && t.Namespace == "ShuffleSeries.ApiGateway.Api.Middlewares"
                && !t.Name.EndsWith("Extensions", StringComparison.Ordinal))
            .ToList();

        middlewareTypes.Should().NotBeEmpty("API Gateway Middlewares altında en az bir middleware sınıfı bulunmalıdır.");

        foreach (var middleware in middlewareTypes)
        {
            middleware.IsSealed.Should().BeTrue(
                $"API Gateway middleware sınıfı '{middleware.Name}' performans ve güvenlik için 'sealed' olmalıdır.");
        }
    }
}
