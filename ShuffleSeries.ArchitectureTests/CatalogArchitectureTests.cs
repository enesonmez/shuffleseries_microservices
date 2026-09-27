using System.Reflection;
using NetArchTest.Rules;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.ArchitectureTests;

public class CatalogArchitectureTests
{
    private static readonly Assembly _domainAssembly = typeof(Series).Assembly;
    private static readonly Assembly _applicationAssembly = typeof(CreateSeriesCommand).Assembly;
    private static readonly Assembly _infrastructureAssembly = typeof(CatalogDbContext).Assembly;
    private static readonly Assembly _apiAssembly = typeof(ShuffleSeries.Catalog.Api.Endpoints.SeriesEndpoints).Assembly;

    #region Layer Dependency Rules (Onion Architecture)

    [Fact]
    public void Domain_ShouldNotHaveDependencyOn_OtherProjects()
    {
        // Domain katmanı hiçbir üst katmana (Application, Infrastructure, Api, Gateway) bağımlı olamaz.
        var otherProjects = new[]
        {
            "ShuffleSeries.Catalog.Application",
            "ShuffleSeries.Catalog.Infrastructure",
            "ShuffleSeries.Catalog.Api",
            "ShuffleSeries.ApiGateway"
        };

        var testResult = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherProjects)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Domain katmanı üst katmanlara bağımlı olamaz. İhlal eden tipler: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_ShouldNotHaveDependencyOn_InfrastructureOrApi()
    {
        // Application katmanı Infrastructure ve Api katmanlarına bağımlı olamaz (Tersine Bağımlılık Prensibi - DIP).
        var forbiddenProjects = new[]
        {
            "ShuffleSeries.Catalog.Infrastructure",
            "ShuffleSeries.Catalog.Api",
            "ShuffleSeries.ApiGateway"
        };

        var testResult = Types.InAssembly(_applicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenProjects)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Application katmanı altyapı veya sunum katmanlarına bağımlı olamaz. İhlal eden tipler: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_ShouldNotHaveDependencyOn_Api()
    {
        // Infrastructure katmanı Api katmanına bağımlı olamaz.
        var testResult = Types.InAssembly(_infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("ShuffleSeries.Catalog.Api")
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Infrastructure katmanı Api katmanına bağımlı olamaz. İhlal eden tipler: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    #endregion

    #region CQRS & MediatR Rules

    [Fact]
    public void Handlers_ShouldHaveNameEndingWith_Handler()
    {
        var testResult = Types.InAssembly(_applicationAssembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"MediatR request handler sınıfları 'Handler' soneki ile bitmelidir. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Handlers_ShouldBeSealed()
    {
        var testResult = Types.InAssembly(_applicationAssembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"MediatR handler sınıfları performans ve güvenlik için 'sealed' olmalıdır. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Validators_ShouldHaveNameEndingWith_Validator()
    {
        var testResult = Types.InAssembly(_applicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"FluentValidation sınıfları 'Validator' soneki ile bitmelidir. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    #endregion

    #region Domain & DDD Rules

    [Fact]
    public void DomainEvents_ShouldImplement_IDomainEvent()
    {
        var testResult = Types.InAssembly(_domainAssembly)
            .That()
            .HaveNameEndingWith("DomainEvent")
            .Should()
            .ImplementInterface(typeof(IDomainEvent))
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Tüm Domain Event sınıfları IDomainEvent arayüzünü uygulamalıdır. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Entities_ShouldInheritFrom_BaseEntityOrAggregateRoot()
    {
        var testResult = Types.InAssembly(_domainAssembly)
            .That()
            .ResideInNamespace("ShuffleSeries.Catalog.Domain.Entities")
            .And()
            .AreClasses()
            .Should()
            .Inherit(typeof(BaseEntity))
            .Or()
            .Inherit(typeof(AggregateRoot))
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Domain içindeki tüm Entity sınıfları BaseEntity veya AggregateRoot temel sınıflarından türemelidir. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Endpoints_ShouldNotDirectlyDependOn_RepositoriesOrDbContext()
    {
        // Minimal API veya Controller sınıfları doğrudan Repository veya DbContext referansı alamaz (CQRS ve MediatR zorunludur).
        var forbiddenDependencies = new[]
        {
            "ShuffleSeries.Catalog.Domain.Repositories",
            "ShuffleSeries.Catalog.Infrastructure.Persistence"
        };

        var testResult = Types.InAssembly(_apiAssembly)
            .That()
            .ResideInNamespace("ShuffleSeries.Catalog.Api.Endpoints")
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDependencies)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"API Endpoint sınıfları doğrudan Repository veya DbContext kullanamaz. CQRS MediatR ISender kullanılmalıdır. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void DomainEntities_Properties_ShouldNotHavePublicSetters()
    {
        // Rich Domain Model kuralı: Entity property'leri dışarıdan doğrudan set edilemez (Encapsulation).
        var entityTypes = _domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "ShuffleSeries.Catalog.Domain.Entities");

        var failingProperties = new List<string>();

        foreach (var type in entityTypes)
        {
            var publicSetProperties = type.GetProperties()
                .Where(p => p.GetSetMethod() != null && p.GetSetMethod()!.IsPublic);

            foreach (var prop in publicSetProperties)
            {
                failingProperties.Add($"{type.Name}.{prop.Name}");
            }
        }

        failingProperties.Should().BeEmpty(
            $"Domain entity özellikleri encapsulation gereği public setter içeremez. İhlaller: {string.Join(", ", failingProperties)}");
    }

    #endregion
}

