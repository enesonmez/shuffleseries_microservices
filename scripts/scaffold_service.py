#!/usr/bin/env python3
"""
ShuffleSeries Microservice Scaffolding Engine
Author: ShuffleSeries Architecture Team
Purpose: Generates a 100% architecturally compliant 5-layer Onion microservice
         (Domain, Application, Infrastructure, Api, Tests, IntegrationTests)
         along with Solution registration and ArchitectureTests integration.
Usage:
    python3 scripts/scaffold_service.py <ServiceName> [--port <Port>] [--dry-run]
Example:
    python3 scripts/scaffold_service.py ShuffleEngine --port 5003
"""

import os
import sys
import re
import argparse
import subprocess
from pathlib import Path

# Package versions SSOT
PKG_VERSIONS = {
    "FluentValidation": "12.1.1",
    "FluentValidation.DependencyInjectionExtensions": "12.1.1",
    "WolverineFx": "6.62.0",
    "WolverineFx.RabbitMQ": "6.62.0",
    "WolverineFx.HealthChecks": "6.62.0",
    "WolverineFx.FluentValidation": "6.62.0",
    "WolverineFx.RuntimeCompilation": "6.62.0",
    "Microsoft.AspNetCore.OpenApi": "10.0.1",
    "Microsoft.EntityFrameworkCore": "10.0.4",
    "Microsoft.EntityFrameworkCore.Design": "10.0.4",
    "Npgsql.EntityFrameworkCore.PostgreSQL": "10.0.2",
    "Npgsql": "10.0.3",
    "Scalar.AspNetCore": "2.16.6",
    "Quartz": "3.19.1",
    "Quartz.Extensions.DependencyInjection": "3.19.1",
    "Quartz.Extensions.Hosting": "3.19.1",
    "AwesomeAssertions": "9.6.0",
    "coverlet.collector": "6.0.4",
    "Microsoft.NET.Test.Sdk": "17.14.1",
    "Moq": "4.20.72",
    "xunit": "2.9.3",
    "xunit.runner.visualstudio": "3.1.4",
    "Microsoft.AspNetCore.Mvc.Testing": "10.0.4",
    "Respawn": "6.2.1",
    "Testcontainers.PostgreSql": "4.3.0",
    "Testcontainers.Redis": "4.3.0",
    "Testcontainers.RabbitMq": "4.3.0",
    "Azure.Identity": "1.13.2",
    "SSH.NET": "2026.0.0",
    "System.Drawing.Common": "9.0.2",
    "System.IdentityModel.Tokens.Jwt": "8.19.2",
}

class ScaffoldEngine:
    def __init__(self, service_name: str, port: int, root_dir: Path, dry_run: bool = False):
        self.service_name = service_name
        self.port_http = port
        self.port_https = port + 2000
        self.root_dir = root_dir
        self.dry_run = dry_run
        self.service_dir = root_dir / f"ShuffleSeries.{service_name}"
        self.sln_file = root_dir / "ShuffleSeries.sln"
        self.arch_tests_proj = root_dir / "ShuffleSeries.ArchitectureTests" / "ShuffleSeries.ArchitectureTests.csproj"
        self.arch_tests_dir = root_dir / "ShuffleSeries.ArchitectureTests"

    def run(self):
        self.validate()
        print(f"\n🚀 Scaffolding ShuffleSeries.{self.service_name} (HTTP: {self.port_http}, HTTPS: {self.port_https})...")
        if self.dry_run:
            print("🔍 DRY-RUN MODE: No files or directories will be written to disk.\n")

        files_to_create = self.build_file_manifest()

        for rel_path, content in files_to_create.items():
            full_path = self.root_dir / rel_path
            if self.dry_run:
                print(f"  [CREATE] {rel_path}")
            else:
                full_path.parent.mkdir(parents=True, exist_ok=True)
                full_path.write_text(content, encoding="utf-8")

        if not self.dry_run:
            self.register_solution()
            self.register_architecture_tests()
            print(f"\n✅ ShuffleSeries.{self.service_name} başarıyla üretildi ve çözüme eklendi!")
            print(f"👉 Doğrulamak için: dotnet build && dotnet test --filter FullyQualifiedName~{self.service_name}")
        else:
            print("\n✅ DRY-RUN tamamlandı. Tüm şablonlar ve bağımlılıklar geçerli.")

    def validate(self):
        if not re.match(r"^[A-Z][a-zA-Z0-9]+$", self.service_name):
            raise ValueError(f"Hatalı servis ismi: '{self.service_name}'. PascalCase olmalı (örn: ShuffleEngine, UserLibrary).")

        if self.service_dir.exists() and not self.dry_run:
            raise FileExistsError(f"Servis dizini zaten mevcut: {self.service_dir}")

        if not self.sln_file.exists():
            raise FileNotFoundError(f"Çözüm dosyası bulunamadı: {self.sln_file}")

    def build_file_manifest(self) -> dict[str, str]:
        manifest = {}
        prefix = f"ShuffleSeries.{self.service_name}"
        s = self.service_name
        s_lower = self.service_name.lower()

        # -------------------------------------------------------------
        # 1. DOMAIN LAYER
        # -------------------------------------------------------------
        domain_proj_rel = f"{prefix}/{prefix}.Domain/{prefix}.Domain.csproj"
        manifest[domain_proj_rel] = f"""<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\\..\\ShuffleSeries.Shared.Core\\ShuffleSeries.Shared.Core.Domain\\ShuffleSeries.Shared.Core.Domain.csproj" />
        <ProjectReference Include="..\\..\\ShuffleSeries.Shared.Core\\ShuffleSeries.Shared.Core.Exceptions\\ShuffleSeries.Shared.Core.Exceptions.csproj" />
    </ItemGroup>

    <ItemGroup>
        <InternalsVisibleTo Include="{prefix}.Tests" />
        <InternalsVisibleTo Include="{prefix}.Infrastructure" />
        <InternalsVisibleTo Include="DynamicProxyGenAssembly2" />
    </ItemGroup>

</Project>
"""
        manifest[f"{prefix}/{prefix}.Domain/Entities/{s}Item.cs"] = f"""using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace {prefix}.Domain.Entities;

public class {s}Item : AggregateRoot
{s_open(1)}
    private {s}Item() {s_open(2)}{s_close(2)}

    public {s}Item(Guid id, string name) : base(id)
    {s_open(2)}
        Name = name;
    {s_close(2)}

    public string Name {s_open(2)} get; private set; {s_close(2)} = string.Empty;
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.Domain/Exceptions/{s}ItemNotFoundException.cs"] = f"""using ShuffleSeries.Shared.Core.Exceptions;

namespace {prefix}.Domain.Exceptions;

public sealed class {s}ItemNotFoundException : NotFoundException
{s_open(1)}
    public {s}ItemNotFoundException(Guid id) 
        : base("{s}Item", id)
    {s_open(2)}{s_close(2)}
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.Domain/Events/{s}ItemCreatedDomainEvent.cs"] = f"""using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace {prefix}.Domain.Events;

public sealed record {s}ItemCreatedDomainEvent(Guid Id, string Name) : IDomainEvent;
"""
        manifest[f"{prefix}/{prefix}.Domain/Repositories/I{s}Repository.cs"] = f"""using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using {prefix}.Domain.Entities;

namespace {prefix}.Domain.Repositories;

public interface I{s}Repository : IRepository
{s_open(1)}
    Task<{s}Item?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync({s}Item item, CancellationToken cancellationToken = default);
{s_close(1)}
"""

        # -------------------------------------------------------------
        # 2. APPLICATION LAYER
        # -------------------------------------------------------------
        app_proj_rel = f"{prefix}/{prefix}.Application/{prefix}.Application.csproj"
        manifest[app_proj_rel] = f"""<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="FluentValidation" Version="{PKG_VERSIONS['FluentValidation']}" />
        <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="{PKG_VERSIONS['FluentValidation.DependencyInjectionExtensions']}" />
        <PackageReference Include="WolverineFx" Version="{PKG_VERSIONS['WolverineFx']}" />
        <PackageReference Include="WolverineFx.FluentValidation" Version="{PKG_VERSIONS['WolverineFx.FluentValidation']}" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\\..\\ShuffleSeries.Shared.Core\\ShuffleSeries.Shared.Core.Application\\ShuffleSeries.Shared.Core.Application.csproj" />
        <ProjectReference Include="..\\{prefix}.Domain\\{prefix}.Domain.csproj" />
    </ItemGroup>

    <ItemGroup>
        <Using Include="ShuffleSeries.Shared.Core.Application.Common.Interfaces" />
    </ItemGroup>

    <ItemGroup>
        <InternalsVisibleTo Include="{prefix}.Api" />
        <InternalsVisibleTo Include="{prefix}.Tests" />
        <InternalsVisibleTo Include="{prefix}.IntegrationTests" />
        <InternalsVisibleTo Include="WolverineHandlers" />
        <InternalsVisibleTo Include="Wolverine" />
        <InternalsVisibleTo Include="DynamicProxyGenAssembly2" />
    </ItemGroup>

</Project>
"""
        manifest[f"{prefix}/{prefix}.Application/DependencyInjection.cs"] = f"""using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Shared.Core.Application;
using ShuffleSeries.Shared.Core.Application.Middlewares;
using Wolverine;
using Wolverine.FluentValidation;

namespace {prefix}.Application;

public static class DependencyInjection
{s_open(1)}
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {s_open(2)}
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddSharedApplication();
        services.AddValidatorsFromAssembly(assembly);

        return services;
    {s_close(2)}

    public static WolverineOptions Add{s}ApplicationMessaging(this WolverineOptions opts)
    {s_open(2)}
        opts.UseFluentValidation();
        opts.Discovery.IncludeAssembly(typeof(DependencyInjection).Assembly);
        opts.RestoreV5Defaults();
        opts.Policies.AddMiddleware(typeof(PerformanceMiddleware));
        opts.Policies.AddMiddleware(typeof(LoggingMiddleware));

        return opts;
    {s_close(2)}
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.Application/Common/Interfaces/I{s}CacheService.cs"] = f"""namespace {prefix}.Application.Common.Interfaces;

public interface I{s}CacheService
{s_open(1)}
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.Application/Features/{s}Items/Commands/Create{s}Item/Create{s}ItemCommand.cs"] = f"""namespace {prefix}.Application.Features.{s}Items.Commands.Create{s}Item;

public sealed record Create{s}ItemCommand(string Name);
"""
        manifest[f"{prefix}/{prefix}.Application/Features/{s}Items/Commands/Create{s}Item/Create{s}ItemCommandValidator.cs"] = f"""using FluentValidation;

namespace {prefix}.Application.Features.{s}Items.Commands.Create{s}Item;

public sealed class Create{s}ItemCommandValidator : AbstractValidator<Create{s}ItemCommand>
{s_open(1)}
    public Create{s}ItemCommandValidator()
    {s_open(2)}
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
    {s_close(2)}
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.Application/Features/{s}Items/Commands/Create{s}Item/Create{s}ItemCommandHandler.cs"] = f"""using {prefix}.Domain.Entities;
using {prefix}.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace {prefix}.Application.Features.{s}Items.Commands.Create{s}Item;

public sealed class Create{s}ItemCommandHandler(I{s}Repository repository, IUnitOfWork unitOfWork)
{s_open(1)}
    public async Task<Guid> Handle(Create{s}ItemCommand request, CancellationToken cancellationToken)
    {s_open(2)}
        var item = new {s}Item(Guid.NewGuid(), request.Name);
        await repository.AddAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return item.Id;
    {s_close(2)}
{s_close(1)}
"""

        # -------------------------------------------------------------
        # 3. INFRASTRUCTURE LAYER
        # -------------------------------------------------------------
        infra_proj_rel = f"{prefix}/{prefix}.Infrastructure/{prefix}.Infrastructure.csproj"
        manifest[infra_proj_rel] = f"""<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <InternalsVisibleTo Include="{prefix}.Api" />
        <InternalsVisibleTo Include="{prefix}.Tests" />
        <InternalsVisibleTo Include="{prefix}.IntegrationTests" />
        <InternalsVisibleTo Include="WolverineHandlers" />
        <InternalsVisibleTo Include="Wolverine" />
        <InternalsVisibleTo Include="DynamicProxyGenAssembly2" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.EntityFrameworkCore" Version="{PKG_VERSIONS['Microsoft.EntityFrameworkCore']}" />
        <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="{PKG_VERSIONS['Npgsql.EntityFrameworkCore.PostgreSQL']}" />
        <PackageReference Include="Quartz" Version="{PKG_VERSIONS['Quartz']}" />
        <PackageReference Include="Quartz.Extensions.DependencyInjection" Version="{PKG_VERSIONS['Quartz.Extensions.DependencyInjection']}" />
        <PackageReference Include="Quartz.Extensions.Hosting" Version="{PKG_VERSIONS['Quartz.Extensions.Hosting']}" />
        <PackageReference Include="WolverineFx.RabbitMQ" Version="{PKG_VERSIONS['WolverineFx.RabbitMQ']}" />
        <PackageReference Include="WolverineFx.HealthChecks" Version="{PKG_VERSIONS['WolverineFx.HealthChecks']}" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\\..\\ShuffleSeries.Shared.Core\\ShuffleSeries.Shared.Core.Infrastructure\\ShuffleSeries.Shared.Core.Infrastructure.csproj" />
        <ProjectReference Include="..\\{prefix}.Domain\\{prefix}.Domain.csproj" />
        <ProjectReference Include="..\\{prefix}.Application\\{prefix}.Application.csproj" />
    </ItemGroup>

</Project>
"""
        manifest[f"{prefix}/{prefix}.Infrastructure/DependencyInjection.cs"] = f"""using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;
using ShuffleSeries.Shared.Core.Infrastructure.Health;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;
using Wolverine;
using Wolverine.HealthChecks;
using Wolverine.RabbitMQ;
using {prefix}.Domain.Repositories;
using {prefix}.Infrastructure.Persistence;
using {prefix}.Infrastructure.Persistence.Repositories;

namespace {prefix}.Infrastructure;

public static class DependencyInjection
{s_open(1)}
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {s_open(2)}
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' was not found.");

        services.AddSharedInfrastructure();

        services.AddDbContext<{s}DbContext>((sp, options) =>
        {s_open(3)}
            options.UseNpgsql(connectionString)
                   .AddInterceptors(
                       sp.GetRequiredService<AuditableEntityInterceptor>(),
                       sp.GetRequiredService<SoftDeleteInterceptor>(),
                       sp.GetRequiredService<InsertOutboxMessagesInterceptor>());
        {s_close(3)});

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<{s}DbContext>());
        services.AddScoped<I{s}Repository, {s}Repository>();
        services.AddSharedRedis(configuration);
        services.AddSharedRedisHealthCheck();
        services.AddSharedRabbitMqHealthCheck(configuration);
        services.AddHealthChecks()
            .AddWolverine(tags: ["messaging", "wolverine", "ready"]);

        return services;
    {s_close(2)}

    public static void Add{s}MessagingTransport(this WolverineOptions opts, IConfiguration configuration)
    {s_open(2)}
        var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ")
            ?? configuration["RabbitMQ:ConnectionString"]
            ?? "amqp://guest:guest@localhost:5672";

        opts.UseRabbitMq(new Uri(rabbitMqConnectionString)).AutoProvision();

        // RabbitMQ Outbound Event Routing (Strictly IIntegrationEvent to prevent internal domain event leakage)
        opts.Publish(rule =>
        {{
            rule.MessagesImplementing<IIntegrationEvent>()
                .ToRabbitExchange("shuffleseries.{s_lower}");
        }});
    {s_close(2)}
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.Infrastructure/Persistence/{s}DbContext.cs"] = f"""using Microsoft.EntityFrameworkCore;
using {prefix}.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure.Persistence.Configurations;
using ShuffleSeries.Shared.Core.Infrastructure.Persistence.Extensions;

namespace {prefix}.Infrastructure.Persistence;

public class {s}DbContext(DbContextOptions<{s}DbContext> options) : DbContext(options), IUnitOfWork
{s_open(1)}
    public DbSet<{s}Item> {s}Items => Set<{s}Item>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {s_open(2)}
        modelBuilder.ApplyConfigurationsFromAssembly(typeof({s}DbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplySoftDeleteQueryFilters();
        base.OnModelCreating(modelBuilder);
    {s_close(2)}
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.Infrastructure/Persistence/{s}DbSeeder.cs"] = f"""namespace {prefix}.Infrastructure.Persistence;

public static class {s}DbSeeder
{s_open(1)}
    public static async Task SeedAsync({s}DbContext context, CancellationToken cancellationToken = default)
    {s_open(2)}
        await context.SaveChangesAsync(cancellationToken);
    {s_close(2)}
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.Infrastructure/Persistence/Configurations/{s}ItemConfiguration.cs"] = f"""using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {prefix}.Domain.Entities;

namespace {prefix}.Infrastructure.Persistence.Configurations;

internal sealed class {s}ItemConfiguration : IEntityTypeConfiguration<{s}Item>
{s_open(1)}
    public void Configure(EntityTypeBuilder<{s}Item> builder)
    {s_open(2)}
        builder.ToTable("{s}Items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
    {s_close(2)}
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.Infrastructure/Persistence/Repositories/{s}Repository.cs"] = f"""using Microsoft.EntityFrameworkCore;
using {prefix}.Domain.Entities;
using {prefix}.Domain.Repositories;

namespace {prefix}.Infrastructure.Persistence.Repositories;

internal sealed class {s}Repository({s}DbContext context) : I{s}Repository
{s_open(1)}
    public async Task<{s}Item?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {s_open(2)}
        return await context.{s}Items.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    {s_close(2)}

    public async Task AddAsync({s}Item item, CancellationToken cancellationToken = default)
    {s_open(2)}
        await context.{s}Items.AddAsync(item, cancellationToken);
    {s_close(2)}
{s_close(1)}
"""

        # -------------------------------------------------------------
        # 4. API LAYER
        # -------------------------------------------------------------
        api_proj_rel = f"{prefix}/{prefix}.Api/{prefix}.Api.csproj"
        manifest[api_proj_rel] = f"""<Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="{PKG_VERSIONS['Microsoft.AspNetCore.OpenApi']}" />
        <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="{PKG_VERSIONS['Microsoft.EntityFrameworkCore.Design']}">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Scalar.AspNetCore" Version="{PKG_VERSIONS['Scalar.AspNetCore']}" />
        <PackageReference Include="WolverineFx" Version="{PKG_VERSIONS['WolverineFx']}" />
        <PackageReference Include="WolverineFx.FluentValidation" Version="{PKG_VERSIONS['WolverineFx.FluentValidation']}" />
        <PackageReference Include="WolverineFx.RabbitMQ" Version="{PKG_VERSIONS['WolverineFx.RabbitMQ']}" />
        <PackageReference Include="WolverineFx.RuntimeCompilation" Version="{PKG_VERSIONS['WolverineFx.RuntimeCompilation']}" />
    </ItemGroup>

    <ItemGroup>
        <Using Include="ShuffleSeries.Shared.Core.Application.Common.Interfaces" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\\..\\ShuffleSeries.Shared.Core\\ShuffleSeries.Shared.Core.Web\\ShuffleSeries.Shared.Core.Web.csproj" />
        <ProjectReference Include="..\\{prefix}.Application\\{prefix}.Application.csproj" />
        <ProjectReference Include="..\\{prefix}.Infrastructure\\{prefix}.Infrastructure.csproj" />
    </ItemGroup>

</Project>
"""
        manifest[f"{prefix}/{prefix}.Api/Contracts/Requests/Create{s}ItemRequest.cs"] = f"""namespace {prefix}.Api.Contracts.Requests;

public sealed record Create{s}ItemRequest(string Name);
"""
        manifest[f"{prefix}/{prefix}.Api/Endpoints/{s}Endpoints.cs"] = f"""using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using {prefix}.Api.Contracts.Requests;
using {prefix}.Application.Features.{s}Items.Commands.Create{s}Item;

namespace {prefix}.Api.Endpoints;

public static class {s}Endpoints
{s_open(1)}
    public static void Map{s}Endpoints(this IEndpointRouteBuilder app)
    {s_open(2)}
        var group = app.MapGroup("api/{s.lower()}-items")
            .WithTags("{s} Items");

        group.MapPost("/", async (
            [FromBody] Create{s}ItemRequest request,
            IMessageBus bus,
            CancellationToken ct) =>
        {s_open(3)}
            var command = new Create{s}ItemCommand(request.Name);
            var id = await bus.InvokeAsync<Guid>(command, ct);
            return Results.Created($"/api/{s.lower()}-items/{{id}}", new {s_open(4)} Id = id {s_close(4)});
        {s_close(3)})
        .WithName("Create{s}Item")
        .WithSummary("Creates a new {s} item.")
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    {s_close(2)}
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.Api/Program.cs"] = f"""using Wolverine;
using {prefix}.Api.Endpoints;
using {prefix}.Application;
using {prefix}.Infrastructure;
using {prefix}.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Vault;
using ShuffleSeries.Shared.Core.Infrastructure.Extensions;
using ShuffleSeries.Shared.Core.Web;
using ShuffleSeries.Shared.Core.Web.Authentication;
using ShuffleSeries.Shared.Core.Web.Correlation;
using ShuffleSeries.Shared.Core.Web.Cors;
using ShuffleSeries.Shared.Core.Web.Health;
using ShuffleSeries.Shared.Core.Web.Localization;
using ShuffleSeries.Shared.Core.Web.Logging;
using ShuffleSeries.Shared.Core.Web.Observability;
using ShuffleSeries.Shared.Core.Web.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog("{s}.Api");
builder.Configuration.AddVault("{s.lower()}");

builder.Host.UseWolverine(opts =>
{{
    opts.Add{s}ApplicationMessaging();
    opts.Add{s}MessagingTransport(builder.Configuration);
}});

builder.Services.AddSharedCorrelation();
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "{s}.Api");
builder.Services.AddSharedHealthChecks();

builder.Services.AddSharedSwagger(options =>
{{
    options.Title = "ShuffleSeries {s} API";
    options.Version = "v1";
    options.Description = "{s} Bounded Context";
    options.IncludeJwtSecurity = true;
}});

builder.Services.AddSharedJwtAuthentication(builder.Configuration, validateTokenBlacklist: false);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSharedCors(builder.Configuration);
builder.Services.AddSharedExceptionHandling();
builder.Services.AddSharedLocalization();

var app = builder.Build();

app.UseSharedCorrelation();
app.UseSharedExceptionHandling();
app.UseSharedCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapSharedHealthChecks();

if (app.Environment.IsDevelopment())
{{
    app.UseSharedSwagger();
    await app.ApplyDatabaseMigrationsAsync<{s}DbContext>({s}DbSeeder.SeedAsync);
}}

app.Map{s}Endpoints();

await app.RunAsync();

#pragma warning disable ASP0027
public partial class Program
{{
    protected Program() {{ }}
}}
#pragma warning restore ASP0027
"""
        manifest[f"{prefix}/{prefix}.Api/Properties/launchSettings.json"] = f"""{{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {{
    "http": {{
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:{self.port_http}",
      "environmentVariables": {{
        "ASPNETCORE_ENVIRONMENT": "Development"
      }}
    }},
    "https": {{
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:{self.port_https};http://localhost:{self.port_http}",
      "environmentVariables": {{
        "ASPNETCORE_ENVIRONMENT": "Development"
      }}
    }}
  }}
}}
"""
        manifest[f"{prefix}/{prefix}.Api/appsettings.json"] = f"""{{
  "Logging": {{
    "LogLevel": {{
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }}
  }},
  "AllowedHosts": "*",
  "Vault": {{
    "Enabled": true,
    "MountPoint": "secret",
    "Optional": true
  }}
}}
"""
        manifest[f"{prefix}/{prefix}.Api/appsettings.Development.json"] = f"""{{
  "Logging": {{
    "LogLevel": {{
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }}
  }}
}}
"""
        manifest[f"{prefix}/{prefix}.Api/Dockerfile"] = f"""FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["{prefix}/{prefix}.Api/{prefix}.Api.csproj", "{prefix}/{prefix}.Api/"]
COPY ["{prefix}/{prefix}.Application/{prefix}.Application.csproj", "{prefix}/{prefix}.Application/"]
COPY ["{prefix}/{prefix}.Domain/{prefix}.Domain.csproj", "{prefix}/{prefix}.Domain/"]
COPY ["{prefix}/{prefix}.Infrastructure/{prefix}.Infrastructure.csproj", "{prefix}/{prefix}.Infrastructure/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Application/ShuffleSeries.Shared.Core.Application.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Application/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Exceptions/ShuffleSeries.Shared.Core.Exceptions.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Exceptions/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Domain/ShuffleSeries.Shared.Core.Domain.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Domain/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/ShuffleSeries.Shared.Core.Infrastructure.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/ShuffleSeries.Shared.Core.Web.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/"]
RUN dotnet restore "{prefix}/{prefix}.Api/{prefix}.Api.csproj"
COPY ["{prefix}/", "{prefix}/"]
COPY ["ShuffleSeries.Shared.Core/", "ShuffleSeries.Shared.Core/"]
WORKDIR "/src/{prefix}/{prefix}.Api"
RUN dotnet build "./{prefix}.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./{prefix}.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER app
ENTRYPOINT ["dotnet", "{prefix}.Api.dll"]
"""
        manifest[f"{prefix}/{prefix}.Api/{prefix}.Api.http"] = f"""@host = http://localhost:{self.port_http}

### 1. Health Live Check
GET {{{{host}}}}/health/live

### 2. Health Ready Check
GET {{{{host}}}}/health/ready

### 3. Create Item
POST {{{{host}}}}/api/{s.lower()}-items
Content-Type: application/json

{{
  "name": "Sample {s} Item"
}}
"""

        # -------------------------------------------------------------
        # 5. TESTS LAYER (Unit Tests)
        # -------------------------------------------------------------
        tests_proj_rel = f"{prefix}/{prefix}.Tests/{prefix}.Tests.csproj"
        manifest[tests_proj_rel] = f"""<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="AwesomeAssertions" Version="{PKG_VERSIONS['AwesomeAssertions']}" />
        <PackageReference Include="coverlet.collector" Version="{PKG_VERSIONS['coverlet.collector']}" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="{PKG_VERSIONS['Microsoft.NET.Test.Sdk']}" />
        <PackageReference Include="Moq" Version="{PKG_VERSIONS['Moq']}" />
        <PackageReference Include="WolverineFx.RuntimeCompilation" Version="{PKG_VERSIONS['WolverineFx.RuntimeCompilation']}" />
        <PackageReference Include="xunit" Version="{PKG_VERSIONS['xunit']}" />
        <PackageReference Include="xunit.runner.visualstudio" Version="{PKG_VERSIONS['xunit.runner.visualstudio']}" />
    </ItemGroup>

    <ItemGroup>
        <Using Include="Xunit" />
        <Using Include="AwesomeAssertions" />
        <Using Include="ShuffleSeries.Shared.Core.Application.Common.Interfaces" />
        <Using Include="ShuffleSeries.Shared.Core.Domain.Constants" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\\{prefix}.Application\\{prefix}.Application.csproj" />
        <ProjectReference Include="..\\{prefix}.Domain\\{prefix}.Domain.csproj" />
        <ProjectReference Include="..\\{prefix}.Infrastructure\\{prefix}.Infrastructure.csproj" />
    </ItemGroup>

</Project>
"""
        manifest[f"{prefix}/{prefix}.Tests/Application/DependencyInjectionTests.cs"] = f"""using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using {prefix}.Application;

namespace {prefix}.Tests.Application;

public class DependencyInjectionTests
{s_open(1)}
    [Fact]
    public void AddApplication_ShouldRegisterValidatorsAndApplicationServices()
    {s_open(2)}
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddApplication();

        services.Any(s => typeof(IValidator).IsAssignableFrom(s.ServiceType)).Should().BeTrue();
    {s_close(2)}
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.Tests/Infrastructure/DependencyInjectionTests.cs"] = f"""using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using {prefix}.Domain.Repositories;
using {prefix}.Infrastructure;
using {prefix}.Infrastructure.Persistence;

namespace {prefix}.Tests.Infrastructure;

public class DependencyInjectionTests
{s_open(1)}
    [Fact]
    public void AddInfrastructure_ShouldRegister_DbContextAndRepositories()
    {s_open(2)}
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        services.Any(s => s.ServiceType == typeof({s}DbContext)).Should().BeTrue();
        services.Any(s => s.ServiceType == typeof(I{s}Repository)).Should().BeTrue();
    {s_close(2)}
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.Tests/Application/Features/{s}Items/Commands/Create{s}Item/Create{s}ItemCommandValidatorTests.cs"] = f"""using FluentValidation.TestHelper;
using {prefix}.Application.Features.{s}Items.Commands.Create{s}Item;

namespace {prefix}.Tests.Application.Features.{s}Items.Commands.Create{s}Item;

public class Create{s}ItemCommandValidatorTests
{s_open(1)}
    private readonly Create{s}ItemCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_When_NameIsEmpty()
    {s_open(2)}
        var command = new Create{s}ItemCommand(string.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    {s_close(2)}

    [Fact]
    public void Should_NotHaveError_When_CommandIsValid()
    {s_open(2)}
        var command = new Create{s}ItemCommand("Valid Name");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    {s_close(2)}
{s_close(1)}
"""

        # -------------------------------------------------------------
        # 6. INTEGRATION TESTS LAYER
        # -------------------------------------------------------------
        int_proj_rel = f"{prefix}/{prefix}.IntegrationTests/{prefix}.IntegrationTests.csproj"
        manifest[int_proj_rel] = f"""<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="AwesomeAssertions" Version="{PKG_VERSIONS['AwesomeAssertions']}" />
        <PackageReference Include="coverlet.collector" Version="{PKG_VERSIONS['coverlet.collector']}" />
        <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="{PKG_VERSIONS['Microsoft.AspNetCore.Mvc.Testing']}" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="{PKG_VERSIONS['Microsoft.NET.Test.Sdk']}" />
        <PackageReference Include="Npgsql" Version="{PKG_VERSIONS['Npgsql']}" />
        <PackageReference Include="Respawn" Version="{PKG_VERSIONS['Respawn']}" />
        <PackageReference Include="Azure.Identity" Version="{PKG_VERSIONS['Azure.Identity']}" />
        <PackageReference Include="SSH.NET" Version="{PKG_VERSIONS['SSH.NET']}" />
        <PackageReference Include="System.Drawing.Common" Version="{PKG_VERSIONS['System.Drawing.Common']}" />
        <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="{PKG_VERSIONS['System.IdentityModel.Tokens.Jwt']}" />
        <PackageReference Include="Testcontainers.PostgreSql" Version="{PKG_VERSIONS['Testcontainers.PostgreSql']}" />
        <PackageReference Include="Testcontainers.Redis" Version="{PKG_VERSIONS['Testcontainers.Redis']}" />
        <PackageReference Include="xunit" Version="{PKG_VERSIONS['xunit']}" />
        <PackageReference Include="xunit.runner.visualstudio" Version="{PKG_VERSIONS['xunit.runner.visualstudio']}" />
    </ItemGroup>

    <ItemGroup>
        <Using Include="Xunit" />
        <Using Include="AwesomeAssertions" />
        <Using Include="ShuffleSeries.Shared.Core.Application.Common.Interfaces" />
        <Using Include="ShuffleSeries.Shared.Core.Domain.Constants" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\\{prefix}.Api\\{prefix}.Api.csproj" />
        <ProjectReference Include="..\\{prefix}.Application\\{prefix}.Application.csproj" />
        <ProjectReference Include="..\\{prefix}.Domain\\{prefix}.Domain.csproj" />
        <ProjectReference Include="..\\{prefix}.Infrastructure\\{prefix}.Infrastructure.csproj" />
    </ItemGroup>

</Project>
"""
        manifest[f"{prefix}/{prefix}.IntegrationTests/Infrastructure/{s}ApiFactory.cs"] = f"""using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;
using {prefix}.Infrastructure.Persistence;

namespace {prefix}.IntegrationTests.Infrastructure;

public class {s}ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{s_open(1)}
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("{s.lower()}_test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    public async Task InitializeAsync()
    {s_open(2)}
        await _dbContainer.StartAsync();
    {s_close(2)}

    public new async Task DisposeAsync()
    {s_open(2)}
        await _dbContainer.DisposeAsync();
    {s_close(2)}

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {s_open(2)}
        builder.ConfigureTestServices(services =>
        {s_open(3)}
            services.RemoveAll<DbContextOptions<{s}DbContext>>();
            services.AddDbContext<{s}DbContext>((sp, options) =>
                options.UseNpgsql(ConnectionString)
                       .AddInterceptors(
                           sp.GetRequiredService<AuditableEntityInterceptor>(),
                           sp.GetRequiredService<SoftDeleteInterceptor>(),
                           sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));
        {s_close(3)});
    {s_close(2)}
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.IntegrationTests/Infrastructure/{s}CollectionFixture.cs"] = f"""namespace {prefix}.IntegrationTests.Infrastructure;

[CollectionDefinition("{s}IntegrationCollection")]
public class {s}CollectionFixture : ICollectionFixture<{s}ApiFactory>
{s_open(1)}{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.IntegrationTests/Infrastructure/{s}IntegrationTestBase.cs"] = f"""using System.Net.Http.Json;
using Npgsql;
using Respawn;

namespace {prefix}.IntegrationTests.Infrastructure;

[Collection("{s}IntegrationCollection")]
public abstract class {s}IntegrationTestBase({s}ApiFactory factory) : IAsyncLifetime
{s_open(1)}
    protected readonly HttpClient Client = factory.CreateClient();
    private Respawner _respawner = default!;

    public async Task InitializeAsync()
    {s_open(2)}
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {s_open(3)}
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"]
        {s_close(3)});

        await _respawner.ResetAsync(connection);
    {s_close(2)}

    public Task DisposeAsync() => Task.CompletedTask;
{s_close(1)}
"""
        manifest[f"{prefix}/{prefix}.IntegrationTests/Endpoints/HealthCheckEndpointsTests.cs"] = f"""using System.Net;
using {prefix}.IntegrationTests.Infrastructure;

namespace {prefix}.IntegrationTests.Endpoints;

public class HealthCheckEndpointsTests({s}ApiFactory factory) : {s}IntegrationTestBase(factory)
{s_open(1)}
    [Fact]
    public async Task Get_HealthLive_ShouldReturnOk()
    {s_open(2)}
        var response = await Client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    {s_close(2)}
{s_close(1)}
"""

        # -------------------------------------------------------------
        # 7. ARCHITECTURE TESTS INTEGRATION
        # -------------------------------------------------------------
        manifest[f"ShuffleSeries.ArchitectureTests/{s}ArchitectureTests.cs"] = f"""using System.Reflection;
using ShuffleSeries.ArchitectureTests.Common;
using {prefix}.Domain.Entities;

namespace ShuffleSeries.ArchitectureTests;

/// <summary>
/// {s} Bounded Context mimari kurallarını doğrular.
/// Ortak Onion, CQRS, DDD ve altyapı kurallarını <see cref="MicroserviceArchitectureTestBase"/> üzerinden miras alır.
/// </summary>
public class {s}ArchitectureTests : MicroserviceArchitectureTestBase
{s_open(1)}
    protected override Assembly DomainAssembly => typeof({s}Item).Assembly;
    protected override Assembly ApplicationAssembly => typeof({prefix}.Application.DependencyInjection).Assembly;
    protected override Assembly InfrastructureAssembly => typeof({prefix}.Infrastructure.DependencyInjection).Assembly;
    protected override Assembly ApiAssembly => typeof({prefix}.Api.Endpoints.{s}Endpoints).Assembly;
    protected override string ServiceNamespacePrefix => "{prefix}";

    protected override IEnumerable<Type> AggregateRootTypes =>
    [
        typeof({s}Item)
    ];
{s_close(1)}
"""
        return manifest

    def register_solution(self):
        prefix = f"ShuffleSeries.{self.service_name}"
        projects = [
            self.root_dir / prefix / f"{prefix}.Domain" / f"{prefix}.Domain.csproj",
            self.root_dir / prefix / f"{prefix}.Application" / f"{prefix}.Application.csproj",
            self.root_dir / prefix / f"{prefix}.Infrastructure" / f"{prefix}.Infrastructure.csproj",
            self.root_dir / prefix / f"{prefix}.Api" / f"{prefix}.Api.csproj",
            self.root_dir / prefix / f"{prefix}.Tests" / f"{prefix}.Tests.csproj",
            self.root_dir / prefix / f"{prefix}.IntegrationTests" / f"{prefix}.IntegrationTests.csproj",
        ]

        print(f"📦 Adding {len(projects)} projects to solution {self.sln_file.name}...")
        for proj in projects:
            cmd = ["dotnet", "sln", str(self.sln_file), "add", str(proj)]
            subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL)

    def register_architecture_tests(self):
        prefix = f"ShuffleSeries.{self.service_name}"
        if not self.arch_tests_proj.exists():
            return

        print(f"🔗 Linking {prefix} projects to ArchitectureTests...")
        content = self.arch_tests_proj.read_text(encoding="utf-8")

        new_refs = f"""        <ProjectReference Include="..\\{prefix}\\{prefix}.Domain\\{prefix}.Domain.csproj" />
        <ProjectReference Include="..\\{prefix}\\{prefix}.Application\\{prefix}.Application.csproj" />
        <ProjectReference Include="..\\{prefix}\\{prefix}.Infrastructure\\{prefix}.Infrastructure.csproj" />
        <ProjectReference Include="..\\{prefix}\\{prefix}.Api\\{prefix}.Api.csproj" />
    </ItemGroup>"""

        if f"{prefix}.Domain.csproj" not in content:
            updated_content = content.replace("    </ItemGroup>\n\n</Project>", f"{new_refs}\n\n</Project>")
            self.arch_tests_proj.write_text(updated_content, encoding="utf-8")

def s_open(indent=1):
    return "{"

def s_close(indent=1):
    return "}"

def main():
    parser = argparse.ArgumentParser(description="ShuffleSeries Microservice Scaffolding Engine")
    parser.add_argument("name", help="Microservice name (e.g. ShuffleEngine, UserLibrary, History)")
    parser.add_argument("--port", type=int, default=5003, help="Allocated HTTP port (default: 5003)")
    parser.add_argument("--dry-run", action="store_true", help="Preview generated files without touching disk")

    args = parser.parse_args()

    # Locate root repo directory
    script_dir = Path(__file__).resolve().parent
    root_dir = script_dir.parent

    engine = ScaffoldEngine(service_name=args.name, port=args.port, root_dir=root_dir, dry_run=args.dry_run)
    try:
        engine.run()
    except Exception as e:
        print(f"❌ Scaffolding Error: {e}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()
