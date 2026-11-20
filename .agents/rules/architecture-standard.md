# Architecture Standard & Symmetry Guard (Demir Kural - Sıfır Asimetri)

Adopt the mindset of a Senior Microservices & Distributed Systems Architect. Every Bounded Context in the ShuffleSeries ecosystem MUST strictly adhere to this Onion Architecture standard without exception.

---

## 1. The 5-Layer Onion Architecture Standard

Every microservice bounded context is composed of 4 core layers plus 2 test suites:

```text
ShuffleSeries.{ServiceName}/
├── ShuffleSeries.{ServiceName}.Domain/
├── ShuffleSeries.{ServiceName}.Application/
├── ShuffleSeries.{ServiceName}.Infrastructure/
├── ShuffleSeries.{ServiceName}.Api/
├── ShuffleSeries.{ServiceName}.Tests/
└── ShuffleSeries.{ServiceName}.IntegrationTests/
```

### 1.1. Domain Layer (`ShuffleSeries.{ServiceName}.Domain`)
* **Purpose:** Pure enterprise business logic, rich domain models, domain events, and repository interfaces.
* **Folder Structure:**
  * `Entities/` - Domain entities inheriting from `BaseEntity` (or `AggregateRoot`).
  * `Enums/` - Strongly-typed domain enumerations.
  * `Events/` - Domain events implementing `IDomainEvent` (`public sealed record`, 1-class-1-file).
  * `Exceptions/` - Sealed domain exceptions inheriting from `CustomException`.
  * `Repositories/` - Repository interfaces implementing `IRepository` marker interface.
* **Strict Rules:**
  * Zero dependencies on upper layers (`Application`, `Infrastructure`, `Api`, `Gateway`).
  * Only references `ShuffleSeries.Shared.Core.Domain` and `ShuffleSeries.Shared.Core.Exceptions`.
  * Entity properties MUST have private or `init` setters (Encapsulation Guard).
  * Repository async methods MUST accept `CancellationToken` as the last parameter.
  * Modern `<InternalsVisibleTo Include="..." />`: `{ServiceName}.Tests`, `{ServiceName}.Infrastructure`, `DynamicProxyGenAssembly2`.

### 1.2. Application Layer (`ShuffleSeries.{ServiceName}.Application`)
* **Purpose:** Use-case orchestration via CQRS (WolverineFx) and input validation (FluentValidation).
* **Folder Structure:**
  * `Common/Interfaces/` - Abstractions consumed by use cases (e.g., cache, external auth).
  * `Common/Models/` - Cross-feature / service-interface internal models ONLY (never feature endpoint responses).
  * `Features/{Feature}/Commands/{Operation}/` - `*Command.cs`, `*CommandHandler.cs`, `*CommandValidator.cs`.
  * `Features/{Feature}/Queries/{Operation}/` - `*Query.cs`, `*QueryHandler.cs`, `*QueryValidator.cs`.
  * `Features/{Feature}/Responses/` - Feature/endpoint response DTOs (`public sealed record`, 1-class-1-file).
  * `DependencyInjection.cs` - Extension methods `AddApplication(this IServiceCollection services)` and `Add{ServiceName}ApplicationMessaging(this WolverineOptions opts)`.
* **Strict Rules:**
  * Zero dependencies on `Infrastructure` or `Api` (DIP).
  * Commands and Queries MUST be `public sealed record` and end with `Command` / `Query`.
  * Handlers MUST be `public sealed class` and end with `Handler` (required by Wolverine Roslyn discovery; outer layers communicate strictly via Wolverine `IMessageBus`).
  * Validators MUST be `public sealed class` and end with `Validator`; every Command MUST have a corresponding `AbstractValidator<TCommand>`.
  * `InternalsVisibleTo`: `{ServiceName}.Tests`, `{ServiceName}.IntegrationTests`.

### 1.3. Infrastructure Layer (`ShuffleSeries.{ServiceName}.Infrastructure`)
* **Purpose:** External concerns, persistence, message broker, distributed cache, and background jobs.
* **Folder Structure:**
  * `BackgroundJobs/` - Quartz jobs implementing `IJob` (`public sealed class`, `[DisallowConcurrentExecution]`, DI-injected `TimeProvider` and `ILogger<T>`).
  * `Persistence/Configurations/` - EF Core `IEntityTypeConfiguration<T>` classes (`internal sealed class`, including `OutboxMessageConfiguration` with partial index `IX_OutboxMessages_Pending`).
  * `Persistence/Repositories/` - `internal sealed class` repository implementations ending with `Repository`, guarding `Update(entity)` with `if (_context.Entry(entity).State == EntityState.Detached)`.
  * `Persistence/{ServiceName}DbContext.cs` - EF Core `DbContext`.
  * `Persistence/{ServiceName}DbSeeder.cs` - `public static class` idempotent data seeder (`ClearDomainEvents()` on seeded entities).
  * `Services/` - External service implementations (pure BCL / `IHttpClientFactory`).
  * `DependencyInjection.cs` - Extension methods `AddInfrastructure(this IServiceCollection services, IConfiguration configuration)` and `Add{ServiceName}MessagingTransport(this WolverineOptions opts, IConfiguration configuration)`.
* **Strict Rules:**
  * Zero dependencies on `Api` AND zero dependencies on `Microsoft.AspNetCore.*` (pure class library without `<FrameworkReference Include="Microsoft.AspNetCore.App" />` or `Microsoft.EntityFrameworkCore.Design`).
  * Repository and `IEntityTypeConfiguration<T>` implementations MUST be `internal sealed`.
  * `InternalsVisibleTo`: `{ServiceName}.Tests`, `{ServiceName}.IntegrationTests`.

### 1.4. API Layer (`ShuffleSeries.{ServiceName}.Api`)
* **Purpose:** Presentation, routing, HTTP contracts, and entry point.
* **Folder Structure:**
  * `Contracts/Requests/{Feature}/` - HTTP request contracts (`public sealed record`).
  * `Endpoints/` - `public static class {Feature}Endpoints` (`Map{Feature}Endpoints(this IEndpointRouteBuilder app)`).
  * `Properties/launchSettings.json` - Allocated unique `http` and `https` profiles.
  * `Program.cs` - Standard bootstrap and shared middleware pipeline.
  * `appsettings.json` & `appsettings.Development.json` (Development contains `LogLevel` overrides only).
  * `Dockerfile` - Multi-stage container definition copying all 4 service `.csproj`s and all 5 `Shared.Core` `.csproj`s before `dotnet restore`.
  * `{ServiceName}.Api.http` - End-to-end HTTP request collections.
* **Strict Rules:**
  * Endpoints MUST NOT inject `DbContext`, Repositories, or Handlers directly; MUST use `IMessageBus` (Wolverine).

---

## 2. Non-Negotiable Symmetry & Quality Mandates

### 2.1. 1-Class-1-File Demir Kuralı
* Asla birden fazla entity, domain event, exception, command, query, validator, handler, options veya response DTO tek bir dosyada birleştirilemez (`bundle` dosyalar yasaktır).
* Dosya adı, içerisindeki sınıf/arayüz/record adı ile karakteri karakterine aynı olmalıdır (`CreateSeriesCommand.cs` -> `public sealed record CreateSeriesCommand(...)`).

### 2.2. SUT Aynalama (System Under Test Mirroring)
* `{ServiceName}.Tests` içerisindeki her test dosyasının klasör hiyerarşisi ve namespace'i, test ettiği sınıfın katmanı ve klasörü ile **1:1** örtüşmelidir:
  * SUT: `ShuffleSeries.Catalog.Application/Features/Movies/Commands/CreateMovie/CreateMovieCommandHandler.cs`
  * Test: `ShuffleSeries.Catalog.Tests/Application/Features/Movies/Commands/CreateMovie/CreateMovieCommandHandlerTests.cs`
* Her serviste mutlaka `Application/DependencyInjectionTests.cs` ve `Infrastructure/DependencyInjectionTests.cs` bulunmalıdır.
* `{ServiceName}.IntegrationTests` içerisindeki `*IntegrationTestBase` ve `*ApiFactory`, sahte/kısmi test seed kodu yazmak yerine doğrudan üretimdeki `{ServiceName}DbSeeder.SeedAsync(context)` metodunu çağırmalıdır.

### 2.3. Scaffolding Motoru Kullanım Zorunluluğu
* Yeni bir mikroservis oluşturulurken klasörler ve `.csproj` dosyaları asla elle açılmaz.
* Doğrudan `python3 scripts/scaffold_service.py <ServiceName> --port <Port>` komutu kullanılır.

### 2.4. Evrensel Mimari Test Mirası
* Yeni her servis için `ShuffleSeries.ArchitectureTests/{ServiceName}ArchitectureTests.cs` oluşturulmalı ve `MicroserviceArchitectureTestBase` sınıfından türetilmelidir.
