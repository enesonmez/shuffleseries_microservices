using System.Reflection;
using NetArchTest.Rules;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.ArchitectureTests;

public class IdentityArchitectureTests
{
    private readonly Assembly _domainAssembly = typeof(User).Assembly;
    private readonly Assembly _applicationAssembly = typeof(ShuffleSeries.Identity.Application.DependencyInjection).Assembly;
    private readonly Assembly _infrastructureAssembly = typeof(ShuffleSeries.Identity.Infrastructure.DependencyInjection).Assembly;
    private readonly Assembly _apiAssembly = typeof(ShuffleSeries.Identity.Api.Endpoints.AuthEndpoints).Assembly;

    #region Layer Dependency Rules (Onion Architecture)

    [Fact]
    public void Domain_ShouldNotHaveDependencyOn_OtherProjects()
    {
        var otherProjects = new[]
        {
            "ShuffleSeries.Identity.Application",
            "ShuffleSeries.Identity.Infrastructure",
            "ShuffleSeries.Identity.Api",
            "ShuffleSeries.ApiGateway"
        };

        var testResult = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherProjects)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Identity Domain katmanı üst katmanlara bağımlı olamaz. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_ShouldNotHaveDependencyOn_InfrastructureOrApi()
    {
        var forbiddenProjects = new[]
        {
            "ShuffleSeries.Identity.Infrastructure",
            "ShuffleSeries.Identity.Api",
            "ShuffleSeries.ApiGateway"
        };

        var testResult = Types.InAssembly(_applicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenProjects)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Identity Application katmanı altyapı veya sunum katmanlarına bağımlı olamaz. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_ShouldNotHaveDependencyOn_Api()
    {
        var testResult = Types.InAssembly(_infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("ShuffleSeries.Identity.Api")
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Identity Infrastructure katmanı Api katmanına bağımlı olamaz. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
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
    public void Endpoints_ShouldNotDirectlyDependOn_RepositoriesOrDbContext()
    {
        var forbiddenDependencies = new[]
        {
            "ShuffleSeries.Identity.Domain.Repositories",
            "ShuffleSeries.Identity.Infrastructure.Persistence"
        };

        var testResult = Types.InAssembly(_apiAssembly)
            .That()
            .ResideInNamespace("ShuffleSeries.Identity.Api.Endpoints")
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDependencies)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"API Endpoint sınıfları doğrudan Repository veya DbContext kullanamaz. CQRS MediatR ISender kullanılmalıdır. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void DomainEntities_Properties_ShouldNotHavePublicSetters()
    {
        var entityTypes = _domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "ShuffleSeries.Identity.Domain.Entities");

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
            $"Identity Domain entity özellikleri encapsulation gereği public setter içeremez. İhlaller: {string.Join(", ", failingProperties)}");
    }

    [Fact]
    public void DomainExceptions_ShouldInheritFromCustomException_AndBeSealed()
    {
        var exceptionTypes = _domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "ShuffleSeries.Identity.Domain.Exceptions")
            .ToList();

        exceptionTypes.Should().NotBeEmpty("Domain katmanında en az bir domain exception bulunmalıdır.");

        foreach (var type in exceptionTypes)
        {
            type.IsSealed.Should().BeTrue($"Domain exception '{type.Name}' sealed olmalıdır.");
            typeof(ShuffleSeries.Shared.Core.Exceptions.CustomException).IsAssignableFrom(type)
                .Should().BeTrue($"Domain exception '{type.Name}' CustomException sınıfından türemelidir.");
        }
    }

    [Fact]
    public void Repositories_ShouldImplement_IRepositoryMarkerInterface()
    {
        var testResult = Types.InAssembly(_domainAssembly)
            .That()
            .ResideInNamespace("ShuffleSeries.Identity.Domain.Repositories")
            .And()
            .AreInterfaces()
            .Should()
            .ImplementInterface(typeof(ShuffleSeries.Shared.Core.Domain.Repositories.IRepository))
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Domain katmanındaki tüm Repository arayüzleri IRepository marker arayüzünü uygulamalıdır. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void AggregateRoots_ShouldInheritFrom_AggregateRoot()
    {
        var aggregateRootTypes = new[] { typeof(User), typeof(Role), typeof(Permission) };

        foreach (var type in aggregateRootTypes)
        {
            typeof(AggregateRoot).IsAssignableFrom(type)
                .Should().BeTrue($"'{type.Name}' AggregateRoot temel sınıfından türemelidir.");
        }
    }

    #endregion

    #region Background Jobs Rules

    [Fact]
    public void BackgroundJobs_Implementing_IJob_ShouldBeSealed_And_HaveDisallowConcurrentExecution()
    {
        var jobTypes = _infrastructureAssembly.GetTypes()
            .Where(t => typeof(Quartz.IJob).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .ToList();

        jobTypes.Should().NotBeEmpty("Altyapıda en az bir Quartz IJob (örn. ProcessOutboxMessagesJob) bulunmalıdır.");

        foreach (var job in jobTypes)
        {
            job.IsSealed.Should().BeTrue($"Quartz IJob sınıfı '{job.Name}' sealed olmalıdır.");

            var hasDisallowConcurrent = Attribute.IsDefined(job, typeof(Quartz.DisallowConcurrentExecutionAttribute));
            hasDisallowConcurrent.Should().BeTrue(
                $"Quartz IJob sınıfı '{job.Name}' çift çalıştırmayı önlemek için [DisallowConcurrentExecution] ile işaretlenmelidir.");
        }
    }

    #endregion
}
