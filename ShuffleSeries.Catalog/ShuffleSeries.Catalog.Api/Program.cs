using ShuffleSeries.Catalog.Api.Endpoints;
using ShuffleSeries.Catalog.Api.Grpc;
using ShuffleSeries.Catalog.Application;
using ShuffleSeries.Catalog.Infrastructure;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
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
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with extensible sinks (Console, File, PostgreSQL)
builder.Host.UseSharedSerilog("Catalog.Api");

// Configure HashiCorp Vault Secrets Management
builder.Configuration.AddVault("catalog");

builder.Host.UseWolverine(opts =>
{
    opts.AddCatalogApplicationMessaging();
});
builder.Host.ConfigureServices((context, services) =>
{
    services.ConfigureWolverine(opts => opts.AddCatalogMessagingTransport(context.Configuration));
});

// Add Distributed Correlation ID Tracking
builder.Services.AddSharedCorrelation();

// Add OpenTelemetry Tracing and Metrics
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Catalog.Api");

// Add Standard Health Checks (Liveness & Readiness)
builder.Services.AddSharedHealthChecks();

// Add Swagger & OpenAPI Documentation
builder.Services.AddSharedSwagger(options =>
{
    options.Title = "ShuffleSeries Catalog API";
    options.Version = "v1";
    options.Description = "Catalog Bounded Context - Movies, Series, Episodes, Platforms, and Moods";
    options.IncludeJwtSecurity = true;
});

// Add Shared JWT Authentication & Authorization (Resolves Jwt:Secret, Jwt:Issuer, Jwt:Audience from Vault/Configuration)
// Edge (Gateway) katmanı kara liste denetimini tek merkezde yaptığı için iç serviste redundant Redis sorgusunu kapatıyoruz
builder.Services.AddSharedJwtAuthentication(builder.Configuration, validateTokenBlacklist: false);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Centralized CORS Policy
builder.Services.AddSharedCors(builder.Configuration);

builder.Services.AddSharedExceptionHandling();

builder.Services.AddSharedLocalization();

builder.Services.AddGrpc(options =>
{
    options.MaxReceiveMessageSize = 1 * 1024 * 1024;
});

var app = builder.Build();

app.UseSharedCorrelation();

app.UseSharedExceptionHandling();

app.UseSharedCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapSharedHealthChecks();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSharedSwagger();
    await app.ApplyDatabaseMigrationsAsync<CatalogDbContext>(
        (context, _) => CatalogDbSeeder.SeedAsync(context));
}

app.MapMovieEndpoints();
app.MapSeriesEndpoints();
app.MapEpisodeEndpoints();
app.MapPlatformEndpoints();
app.MapMoodEndpoints();
app.MapGenreEndpoints();
app.MapMediaAssociationEndpoints();
app.MapAdminMediaEndpoints();
app.MapGrpcService<CatalogGrpcService>();

await app.RunAsync();

#pragma warning disable ASP0027 // Required for WebApplicationFactory<Program> in integration test projects
public partial class Program
{
    protected Program()
    {
    }
}
#pragma warning restore ASP0027
