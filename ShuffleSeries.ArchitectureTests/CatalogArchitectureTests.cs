using System.Reflection;
using NetArchTest.Rules;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Exceptions;

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

    [Fact]
    public void Commands_ShouldHaveNameEndingWith_Command()
    {
        var commandTypes = _applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.Name.Contains("<>")
                && t.GetInterfaces().Any(i => i == typeof(MediatR.IRequest) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>)))
                && t.Namespace != null && t.Namespace.Contains(".Commands."))
            .ToList();

        commandTypes.Should().NotBeEmpty("Catalog Application katmanında en az bir command bulunmalıdır.");

        foreach (var type in commandTypes)
        {
            type.Name.Should().EndWith("Command", $"'{type.FullName}' sınıfı 'Command' ile bitmelidir.");
        }
    }

    [Fact]
    public void Queries_ShouldHaveNameEndingWith_Query()
    {
        var queryTypes = _applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.Name.Contains("<>")
                && t.GetInterfaces().Any(i => i == typeof(MediatR.IRequest) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>)))
                && t.Namespace != null && t.Namespace.Contains(".Queries."))
            .ToList();

        queryTypes.Should().NotBeEmpty("Catalog Application katmanında en az bir query bulunmalıdır.");

        foreach (var type in queryTypes)
        {
            type.Name.Should().EndWith("Query", $"'{type.FullName}' sınıfı 'Query' ile bitmelidir.");
        }
    }

    [Fact]
    public void Commands_ShouldHave_CorrespondingValidator()
    {
        var commandTypes = _applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.Name.Contains("<>")
                && t.GetInterfaces().Any(i => i == typeof(MediatR.IRequest) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>)))
                && t.Namespace != null && t.Namespace.Contains(".Commands."))
            .ToList();

        var validatorTypes = _applicationAssembly.GetTypes()
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
            $"Her CQRS Command için bir FluentValidation Validator sınıfı bulunmalıdır. Validator'ı eksik olan komutlar: {string.Join(", ", missingValidators)}");
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
    public void AggregateRoots_ShouldInheritFrom_AggregateRoot()
    {
        var aggregateRootTypes = new[]
        {
            typeof(Media),
            typeof(Series),
            typeof(Movie),
            typeof(Platform),
            typeof(Mood),
            typeof(Genre)
        };

        foreach (var type in aggregateRootTypes)
        {
            typeof(AggregateRoot).IsAssignableFrom(type)
                .Should().BeTrue($"Aggregate root '{type.Name}' AggregateRoot sınıfından türemelidir.");
        }
    }

    [Fact]
    public void Entities_OtherThanAggregateRoots_ShouldNotInheritFrom_AggregateRoot()
    {
        var childEntities = new[]
        {
            typeof(Episode),
            typeof(Season),
            typeof(EpisodePlatform),
            typeof(EpisodeTranslation),
            typeof(MediaPlatform),
            typeof(MediaMood),
            typeof(MediaGenre),
            typeof(MediaTranslation)
        };

        foreach (var entity in childEntities)
        {
            typeof(AggregateRoot).IsAssignableFrom(entity)
                .Should().BeFalse($"Child entity '{entity.Name}' bir AggregateRoot olamaz; BaseEntity olmalıdır.");
        }
    }

    [Fact]
    public void RepositoryMethods_ShouldHave_CancellationTokenAsLastParameter()
    {
        var repoInterfaces = _domainAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace == "ShuffleSeries.Catalog.Domain.Repositories" && typeof(ShuffleSeries.Shared.Core.Domain.Repositories.IRepository).IsAssignableFrom(t))
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
            $"Tüm asenkron repository metotlarının son parametresi CancellationToken olmalıdır (.NET 10 CLR & ThreadPool Güvenliği). İhlal eden metotlar: {string.Join(", ", violatingMethods)}");
    }

    [Fact]
    public void DomainExceptions_ShouldInheritFromCustomException_AndBeSealed()
    {
        var exceptionTypes = _domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "ShuffleSeries.Catalog.Domain.Exceptions")
            .ToList();

        exceptionTypes.Should().NotBeEmpty("Catalog Domain katmanında en az bir domain exception sınıfı bulunmalıdır.");

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

    [Fact]
    public void Repositories_ShouldImplement_IRepositoryMarkerInterface()
    {
        var testResult = Types.InAssembly(_domainAssembly)
            .That()
            .ResideInNamespace("ShuffleSeries.Catalog.Domain.Repositories")
            .And()
            .AreInterfaces()
            .Should()
            .ImplementInterface(typeof(ShuffleSeries.Shared.Core.Domain.Repositories.IRepository))
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue(
            $"Catalog Domain katmanındaki tüm Repository arayüzleri IRepository marker arayüzünü uygulamalıdır. İhlaller: {string.Join(", ", testResult.FailingTypeNames ?? [])}");
    }

    #endregion

    #region Background Jobs Rules

    [Fact]
    public void Repositories_InInfrastructure_ShouldHaveNameEndingWith_Repository_And_BeInternalSealed()
    {
        var repoTypes = _infrastructureAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsNested && !t.Name.Contains("<") && t.Namespace == "ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories")
            .ToList();

        repoTypes.Should().NotBeEmpty("Altyapıda en az bir repository implementasyonu bulunmalıdır.");

        foreach (var type in repoTypes)
        {
            type.Name.Should().EndWith("Repository", $"Repository sınıfı '{type.Name}' 'Repository' soneki ile bitmelidir.");
            type.IsSealed.Should().BeTrue($"Repository sınıfı '{type.Name}' sealed olmalıdır.");
            type.IsNotPublic.Should().BeTrue($"Repository sınıfı '{type.Name}' internal (not public) olmalıdır.");
        }
    }

    [Fact]
    public void EntityTypeConfigurations_ShouldHaveNameEndingWith_Configuration_And_BeInConfigurationsNamespace()
    {
        var configTypes = _infrastructureAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<>)))
            .ToList();

        configTypes.Should().NotBeEmpty("Altyapıda en az bir EF Core EntityTypeConfiguration bulunmalıdır.");

        foreach (var type in configTypes)
        {
            type.Name.Should().EndWith("Configuration", $"Configuration sınıfı '{type.Name}' 'Configuration' ile bitmelidir.");
            type.Namespace.Should().Be("ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations");
        }
    }

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

