using System.Reflection;
using NetArchTest.Rules;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.ArchitectureTests.Common;

/// <summary>
/// Tüm ShuffleSeries mikroservislerinin Onion Architecture, CQRS, DDD ve güvenlik/performans
/// kurallarına tam uyumunu garanti eden evrensel mimari test taban sınıfı.
/// Her Bounded Context bu sınıftan türeyerek sıfır kod tekrarı ile tüm mimari korumalara sahip olur.
/// </summary>
public abstract class MicroserviceArchitectureTestBase
{
    protected abstract Assembly DomainAssembly { get; }
    protected abstract Assembly ApplicationAssembly { get; }
    protected abstract Assembly InfrastructureAssembly { get; }
    protected abstract Assembly ApiAssembly { get; }
    protected abstract string ServiceNamespacePrefix { get; }

    /// <summary>
    /// Bileşik birincil anahtara (composite primary key) sahip junction tabloları (örn. UserRole) BaseEntity'den türemez.
    /// Alt sınıflar bu tip entity isimlerini buraya ekleyebilir.
    /// </summary>
    protected virtual IEnumerable<string> CompositeKeyEntityExclusionNames => [];

    /// <summary>
    /// Bounded Context içerisindeki AggregateRoot sınıfları listesi.
    /// </summary>
    protected virtual IEnumerable<Type> AggregateRootTypes => [];

    /// <summary>
    /// AggregateRoot olmayan ve AggregateRoot'tan türememesi gereken alt entity sınıfları listesi.
    /// </summary>
    protected virtual IEnumerable<Type> ChildEntityTypes => [];

    #region Layer Dependency Rules (Onion Architecture)

    [Fact]
    public void Domain_ShouldNotHaveDependencyOn_OtherProjects()
    {
        var otherProjects = new[]
        {
            $"{ServiceNamespacePrefix}.Application",
            $"{ServiceNamespacePrefix}.Infrastructure",
            $"{ServiceNamespacePrefix}.Api",
            "ShuffleSeries.ApiGateway"
        };

        var testResult = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherProjects)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"{ServiceNamespacePrefix} Domain katmanı hiçbir üst katmana bağımlı olamaz. İhlal eden tipler: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_ShouldNotHaveDependencyOn_InfrastructureOrApi()
    {
        var forbiddenProjects = new[]
        {
            $"{ServiceNamespacePrefix}.Infrastructure",
            $"{ServiceNamespacePrefix}.Api",
            "ShuffleSeries.ApiGateway"
        };

        var testResult = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenProjects)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"{ServiceNamespacePrefix} Application katmanı altyapı veya sunum katmanlarına bağımlı olamaz (DIP kuralı). İhlal eden tipler: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_ShouldNotHaveDependencyOn_Api()
    {
        var testResult = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn($"{ServiceNamespacePrefix}.Api")
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"{ServiceNamespacePrefix} Infrastructure katmanı Api katmanına bağımlı olamaz. İhlal eden tipler: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_ShouldNotHaveDependencyOn_AspNetCore()
    {
        var testResult = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"{ServiceNamespacePrefix} Infrastructure katmanı ASP.NET Core web çatısına (Microsoft.AspNetCore.*) bağımlı olamaz; saf altyapı kütüphanesi kalmalıdır. İhlal eden tipler: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    #endregion

    #region CQRS & MediatR Rules

    private List<Type> GetHandlerTypes() =>
        ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsNested: false } && !t.Name.Contains('<')
                && t.GetInterfaces().Any(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<>))))
            .ToList();

    [Fact]
    public void Handlers_ShouldHaveNameEndingWith_Handler()
    {
        var handlerTypes = GetHandlerTypes();
        if (handlerTypes.Count == 0) return;

        foreach (var type in handlerTypes)
        {
            type.Name.Should().EndWith("Handler",
                $"{ServiceNamespacePrefix} MediatR request handler sınıfı '{type.FullName}' 'Handler' soneki ile bitmelidir.");
        }
    }

    [Fact]
    public void Handlers_ShouldBeSealed()
    {
        var handlerTypes = GetHandlerTypes();
        if (handlerTypes.Count == 0) return;

        foreach (var type in handlerTypes)
        {
            type.IsSealed.Should().BeTrue(
                $"{ServiceNamespacePrefix} MediatR handler sınıfı '{type.FullName}' performans ve güvenlik için 'sealed' olmalıdır.");
        }
    }

    [Fact]
    public void Handlers_ShouldBeInternal()
    {
        var handlerTypes = GetHandlerTypes();
        if (handlerTypes.Count == 0) return;

        foreach (var type in handlerTypes)
        {
            type.IsNotPublic.Should().BeTrue(
                $"{ServiceNamespacePrefix} MediatR handler sınıfı '{type.FullName}' kapsülleme gereği 'internal' olmalıdır (dış katmanlar yalnızca ISender kullanmalıdır).");
        }
    }

    [Fact]
    public void CommandsAndQueries_ShouldBeSealed()
    {
        var requestTypes = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && !t.Name.Contains("<>")
                && t.GetInterfaces().Any(i => i == typeof(MediatR.IRequest) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>))))
            .ToList();

        if (requestTypes.Count == 0) return;

        foreach (var type in requestTypes)
        {
            type.IsSealed.Should().BeTrue(
                $"{ServiceNamespacePrefix} CQRS Command/Query kaydı '{type.FullName}' 'sealed' olmalıdır.");
        }
    }

    [Fact]
    public void Validators_ShouldHaveNameEndingWith_Validator()
    {
        var testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"{ServiceNamespacePrefix} FluentValidation sınıfları 'Validator' soneki ile bitmelidir. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Validators_ShouldBeSealed()
    {
        var testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"{ServiceNamespacePrefix} FluentValidation Validator sınıfları 'sealed' olmalıdır. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Commands_ShouldHaveNameEndingWith_Command()
    {
        var commandTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.Name.Contains("<>")
                && t.GetInterfaces().Any(i => i == typeof(MediatR.IRequest) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>)))
                && t.Namespace != null && t.Namespace.Contains(".Commands."))
            .ToList();

        if (commandTypes.Count == 0) return;

        foreach (var type in commandTypes)
        {
            type.Name.Should().EndWith("Command", $"'{type.FullName}' sınıfı 'Command' ile bitmelidir.");
        }
    }

    [Fact]
    public void Queries_ShouldHaveNameEndingWith_Query()
    {
        var queryTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.Name.Contains("<>")
                && t.GetInterfaces().Any(i => i == typeof(MediatR.IRequest) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>)))
                && t.Namespace != null && t.Namespace.Contains(".Queries."))
            .ToList();

        if (queryTypes.Count == 0) return;

        foreach (var type in queryTypes)
        {
            type.Name.Should().EndWith("Query", $"'{type.FullName}' sınıfı 'Query' ile bitmelidir.");
        }
    }

    [Fact]
    public void Commands_ShouldHave_CorrespondingValidator()
    {
        var commandTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.Name.Contains("<>")
                && t.GetInterfaces().Any(i => i == typeof(MediatR.IRequest) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>)))
                && t.Namespace != null && t.Namespace.Contains(".Commands."))
            .ToList();

        var validatorTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.BaseType is { IsGenericType: true } && t.BaseType.GetGenericTypeDefinition() == typeof(FluentValidation.AbstractValidator<>))
            .ToList();

        var missingValidators = new List<string>();

        foreach (var commandType in commandTypes)
        {
            var hasValidator = validatorTypes.Any(v => v.BaseType!.GetGenericArguments()[0] == commandType);
            if (!hasValidator)
            {
                missingValidators.Add(commandType.FullName ?? commandType.Name);
            }
        }

        missingValidators.Should().BeEmpty(
            $"{ServiceNamespacePrefix} CQRS Command'leri için FluentValidation Validator sınıfı bulunmalıdır. Validator'ı eksik olan komutlar: {string.Join(", ", missingValidators)}");
    }

    #endregion

    #region Domain & DDD Rules

    [Fact]
    public void DomainEvents_ShouldImplement_IDomainEvent()
    {
        var testResult = Types.InAssembly(DomainAssembly)
            .That()
            .HaveNameEndingWith("DomainEvent")
            .Should()
            .ImplementInterface(typeof(IDomainEvent))
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"{ServiceNamespacePrefix} Tüm Domain Event sınıfları IDomainEvent arayüzünü uygulamalıdır. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void DomainEvents_ShouldBeSealed()
    {
        var testResult = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"{ServiceNamespacePrefix} Tüm Domain Event sınıfları sealed olmalıdır. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Entities_ShouldInheritFrom_BaseEntityOrAggregateRoot()
    {
        var predicate = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace($"{ServiceNamespacePrefix}.Domain.Entities")
            .And()
            .AreClasses();

        foreach (var exclusion in CompositeKeyEntityExclusionNames)
        {
            predicate = predicate.And().DoNotHaveName(exclusion);
        }

        var testResult = predicate
            .Should()
            .Inherit(typeof(BaseEntity))
            .Or()
            .Inherit(typeof(AggregateRoot))
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"{ServiceNamespacePrefix} Domain içindeki tüm Entity sınıfları BaseEntity veya AggregateRoot temel sınıflarından türemelidir. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void AggregateRoots_ShouldInheritFrom_AggregateRoot()
    {
        var aggregateRoots = AggregateRootTypes.ToList();
        if (aggregateRoots.Count == 0) return;

        foreach (var type in aggregateRoots)
        {
            typeof(AggregateRoot).IsAssignableFrom(type)
                .Should().BeTrue($"Aggregate root '{type.Name}' AggregateRoot sınıfından türemelidir.");
        }
    }

    [Fact]
    public void Entities_OtherThanAggregateRoots_ShouldNotInheritFrom_AggregateRoot()
    {
        var childEntities = ChildEntityTypes.ToList();
        if (childEntities.Count == 0) return;

        foreach (var entity in childEntities)
        {
            typeof(AggregateRoot).IsAssignableFrom(entity)
                .Should().BeFalse($"Child entity '{entity.Name}' bir AggregateRoot olamaz; BaseEntity olmalıdır.");
        }
    }

    [Fact]
    public void AllDomainEntities_ShouldBeRegisteredAsAggregateRootOrChildEntity()
    {
        var registeredTypes = new HashSet<Type>(AggregateRootTypes.Concat(ChildEntityTypes));
        var concreteEntities = DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.Namespace == $"{ServiceNamespacePrefix}.Domain.Entities" &&
                        !t.Name.Contains("<"))
            .ToList();

        var unregisteredEntities = concreteEntities
            .Where(t => !registeredTypes.Contains(t))
            .Select(t => t.Name)
            .ToList();

        unregisteredEntities.Should().BeEmpty(
            $"{ServiceNamespacePrefix} Domain içindeki tüm somut entity sınıfları test sınıfında AggregateRootTypes veya ChildEntityTypes listesinde kayıtlı olmalıdır. Eksik olanlar: {string.Join(", ", unregisteredEntities)}");
    }

    [Fact]
    public void ApiRequests_And_ApplicationResponses_ShouldBePublicSealed()
    {
        var requestTypes = ApiAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.Namespace != null &&
                        t.Namespace.StartsWith($"{ServiceNamespacePrefix}.Api.Contracts.Requests", StringComparison.Ordinal) &&
                        !t.Name.Contains("<"))
            .ToList();

        foreach (var req in requestTypes)
        {
            req.IsPublic.Should().BeTrue($"API request DTO '{req.Name}' public olmalıdır.");
            req.IsSealed.Should().BeTrue($"API request DTO '{req.Name}' sealed olmalıdır.");
        }

        var responseTypes = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.Namespace != null &&
                        t.Namespace.Contains(".Responses") &&
                        !t.Name.Contains("<"))
            .ToList();

        foreach (var resp in responseTypes)
        {
            resp.IsPublic.Should().BeTrue($"Application response DTO '{resp.Name}' public olmalıdır.");
            resp.IsSealed.Should().BeTrue($"Application response DTO '{resp.Name}' sealed olmalıdır.");
        }
    }

    [Fact]
    public void RepositoryMethods_ShouldHave_CancellationTokenAsLastParameter()
    {
        var repoInterfaces = DomainAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace == $"{ServiceNamespacePrefix}.Domain.Repositories" && typeof(IRepository).IsAssignableFrom(t))
            .ToList();

        var violatingMethods = new List<string>();

        foreach (var repoInterface in repoInterfaces)
        {
            var methods = repoInterface.GetMethods()
                .Where(m => typeof(Task).IsAssignableFrom(m.ReturnType));

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 0 || parameters[^1].ParameterType != typeof(CancellationToken))
                {
                    violatingMethods.Add($"{repoInterface.Name}.{method.Name}");
                }
            }
        }

        violatingMethods.Should().BeEmpty(
            $"{ServiceNamespacePrefix} Tüm asenkron repository metotlarının son parametresi CancellationToken olmalıdır (.NET 10 CLR & ThreadPool Güvenliği). İhlal eden metotlar: {string.Join(", ", violatingMethods)}");
    }

    [Fact]
    public void DomainExceptions_ShouldInheritFromCustomException_AndBeSealed()
    {
        var exceptionTypes = DomainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == $"{ServiceNamespacePrefix}.Domain.Exceptions")
            .ToList();

        if (exceptionTypes.Count == 0) return;

        foreach (var exceptionType in exceptionTypes)
        {
            typeof(CustomException).IsAssignableFrom(exceptionType)
                .Should().BeTrue($"Domain exception '{exceptionType.Name}' CustomException sınıfından türemelidir.");

            exceptionType.IsSealed
                .Should().BeTrue($"Domain exception '{exceptionType.Name}' sealed olmalıdır.");
        }
    }

    [Fact]
    public void Endpoints_ShouldNotDirectlyDependOn_RepositoriesOrDbContext()
    {
        var forbiddenDependencies = new[]
        {
            $"{ServiceNamespacePrefix}.Domain.Repositories",
            $"{ServiceNamespacePrefix}.Infrastructure.Persistence"
        };

        var testResult = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace($"{ServiceNamespacePrefix}.Api.Endpoints")
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDependencies)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"{ServiceNamespacePrefix} API Endpoint sınıfları doğrudan Repository veya DbContext kullanamaz. CQRS MediatR ISender kullanılmalıdır. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void DomainEntities_Properties_ShouldNotHavePublicSetters()
    {
        var entityTypes = DomainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == $"{ServiceNamespacePrefix}.Domain.Entities");

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
            $"{ServiceNamespacePrefix} Domain entity özellikleri encapsulation gereği public setter içeremez. İhlaller: {string.Join(", ", failingProperties)}");
    }

    [Fact]
    public void Repositories_ShouldImplement_IRepositoryMarkerInterface()
    {
        var testResult = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace($"{ServiceNamespacePrefix}.Domain.Repositories")
            .And()
            .AreInterfaces()
            .Should()
            .ImplementInterface(typeof(IRepository))
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"{ServiceNamespacePrefix} Domain katmanındaki tüm Repository arayüzleri IRepository marker arayüzünü uygulamalıdır. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    #endregion

    #region Infrastructure & Persistence Rules

    [Fact]
    public void Repositories_InInfrastructure_ShouldHaveNameEndingWith_Repository_And_BeInternalSealed()
    {
        var repoTypes = InfrastructureAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsNested && !t.Name.Contains("<") && t.Namespace == $"{ServiceNamespacePrefix}.Infrastructure.Persistence.Repositories")
            .ToList();

        if (repoTypes.Count == 0) return;

        foreach (var type in repoTypes)
        {
            type.Name.Should().EndWith("Repository", $"Repository sınıfı '{type.Name}' 'Repository' soneki ile bitmelidir.");
            type.IsSealed.Should().BeTrue($"Repository sınıfı '{type.Name}' sealed olmalıdır.");
            type.IsNotPublic.Should().BeTrue($"Repository sınıfı '{type.Name}' internal (not public) olmalıdır.");
        }
    }

    [Fact]
    public void EntityTypeConfigurations_ShouldHaveNameEndingWith_Configuration_And_BeInConfigurationsNamespace_And_BeInternalSealed()
    {
        var configTypes = InfrastructureAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<>)))
            .ToList();

        if (configTypes.Count == 0) return;

        foreach (var type in configTypes)
        {
            type.Name.Should().EndWith("Configuration", $"Configuration sınıfı '{type.Name}' 'Configuration' ile bitmelidir.");
            type.Namespace.Should().Be($"{ServiceNamespacePrefix}.Infrastructure.Persistence.Configurations");
            type.IsSealed.Should().BeTrue($"Configuration sınıfı '{type.Name}' 'sealed' olmalıdır.");
            type.IsNotPublic.Should().BeTrue($"Configuration sınıfı '{type.Name}' 'internal' (not public) olmalıdır.");
        }
    }

    [Fact]
    public void BackgroundJobs_Implementing_IJob_ShouldBeSealed_And_HaveDisallowConcurrentExecution()
    {
        var jobTypes = InfrastructureAssembly.GetTypes()
            .Where(t => typeof(Quartz.IJob).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .ToList();

        if (jobTypes.Count == 0) return;

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
