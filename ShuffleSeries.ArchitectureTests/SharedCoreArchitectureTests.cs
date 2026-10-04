using System.Reflection;
using NetArchTest.Rules;
using ShuffleSeries.Shared.Core.Application.Requests;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Exceptions;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;
using ShuffleSeries.Shared.Core.Web.Middlewares;

namespace ShuffleSeries.ArchitectureTests;

public class SharedCoreArchitectureTests
{
    private static readonly Assembly _sharedDomainAssembly = typeof(BaseEntity).Assembly;
    private static readonly Assembly _sharedApplicationAssembly = typeof(PaginationRequest).Assembly;
    private static readonly Assembly _sharedExceptionsAssembly = typeof(CustomException).Assembly;
    private static readonly Assembly _sharedInfrastructureAssembly = typeof(SoftDeleteInterceptor).Assembly;
    private static readonly Assembly _sharedWebAssembly = typeof(GlobalExceptionHandler).Assembly;

    [Fact]
    public void SharedCoreDomain_ShouldNotHaveDependencyOn_OtherSharedLayers()
    {
        var otherLayers = new[]
        {
            "ShuffleSeries.Shared.Core.Application",
            "ShuffleSeries.Shared.Core.Infrastructure",
            "ShuffleSeries.Shared.Core.Web"
        };

        var testResult = Types.InAssembly(_sharedDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherLayers)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Shared.Core.Domain katmanı Application, Infrastructure veya Web katmanlarına bağımlı olamaz. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void SharedCoreApplication_ShouldNotHaveDependencyOn_InfrastructureOrWeb()
    {
        var forbiddenLayers = new[]
        {
            "ShuffleSeries.Shared.Core.Infrastructure",
            "ShuffleSeries.Shared.Core.Web"
        };

        var testResult = Types.InAssembly(_sharedApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenLayers)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Shared.Core.Application katmanı Infrastructure veya Web katmanlarına bağımlı olamaz. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void SharedCoreExceptions_ShouldNotHaveDependencyOn_ApplicationInfrastructureOrWeb()
    {
        var forbiddenLayers = new[]
        {
            "ShuffleSeries.Shared.Core.Application",
            "ShuffleSeries.Shared.Core.Infrastructure",
            "ShuffleSeries.Shared.Core.Web"
        };

        var testResult = Types.InAssembly(_sharedExceptionsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenLayers)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Shared.Core.Exceptions katmanı bağımsız bir çekirdek olmalıdır. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Behaviors_In_SharedCoreApplication_ShouldBeSealed()
    {
        var testResult = Types.InAssembly(_sharedApplicationAssembly)
            .That()
            .ImplementInterface(typeof(MediatR.IPipelineBehavior<,>))
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Shared.Core.Application içindeki IPipelineBehavior uygulayan sınıflar 'sealed' olmalıdır. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void IRepositoryGeneric_ShouldInheritFrom_IRepositoryMarker()
    {
        typeof(ShuffleSeries.Shared.Core.Domain.Repositories.IRepository)
            .IsAssignableFrom(typeof(ShuffleSeries.Shared.Core.Domain.Repositories.IRepository<>))
            .Should().BeTrue("Generic IRepository<TEntity> arayüzü IRepository marker arayüzünden türemelidir.");
    }
}
