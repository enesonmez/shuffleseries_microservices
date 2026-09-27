using ShuffleSeries.Catalog.Api.Endpoints;
using ShuffleSeries.Catalog.Api.Extensions;
using ShuffleSeries.Catalog.Application;
using ShuffleSeries.Catalog.Infrastructure;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Vault;
using ShuffleSeries.Shared.Core.Web;
using ShuffleSeries.Shared.Core.Web.Correlation;
using ShuffleSeries.Shared.Core.Web.Cors;
using ShuffleSeries.Shared.Core.Web.Health;
using ShuffleSeries.Shared.Core.Web.Logging;
using ShuffleSeries.Shared.Core.Web.Observability;
using ShuffleSeries.Shared.Core.Web.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with extensible sinks (Console, File, PostgreSQL)
builder.Host.UseSharedSerilog("Catalog.Api");

// Configure HashiCorp Vault Secrets Management
builder.Configuration.AddVault("catalog");

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

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Centralized CORS Policy
builder.Services.AddSharedCors(builder.Configuration);

builder.Services.AddSharedExceptionHandling();

var app = builder.Build();

app.UseSharedCorrelation();

app.UseSharedExceptionHandling();

app.UseSharedCors();

app.MapSharedHealthChecks();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSharedSwagger();
}

app.MapSeriesEndpoints();

await app.ApplyMigrationsAsync();

await app.RunAsync();

#pragma warning disable ASP0027 // Required for WebApplicationFactory<Program> in integration test projects
public partial class Program
{
    protected Program()
    {
    }
}
#pragma warning restore ASP0027
