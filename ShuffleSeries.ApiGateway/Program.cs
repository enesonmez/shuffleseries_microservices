using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Vault;
using ShuffleSeries.Shared.Core.Web.Correlation;
using ShuffleSeries.Shared.Core.Web.Cors;
using ShuffleSeries.Shared.Core.Web.Health;
using ShuffleSeries.Shared.Core.Web.Logging;
using ShuffleSeries.Shared.Core.Web.Observability;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with extensible sinks (Console, File, PostgreSQL)
builder.Host.UseSharedSerilog("ApiGateway");

// Configure HashiCorp Vault Secrets Management
builder.Configuration.AddVault("apigateway");

// Add Distributed Correlation ID Tracking
builder.Services.AddSharedCorrelation();

// Add OpenTelemetry Tracing and Metrics
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "ApiGateway");

// Add Health Checks
builder.Services.AddSharedHealthChecks();

// Add Centralized CORS Policy
builder.Services.AddSharedCors(builder.Configuration);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseSharedCorrelation();

app.UseSharedCors();

app.MapSharedHealthChecks();

app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "ShuffleSeries API Gateway - Aggregated Documentation";

    // Mikroservislerin OpenAPI v3 spesifikasyonları Gateway dropdown'ına eklenir:
    options.SwaggerEndpoint("/catalog-api/openapi/v1.json", "Catalog Service API (v1)");
    // Not: Gelecekte eklenecek mikroservislerin OpenAPI spesifikasyonları buraya SwaggerEndpoint olarak dahil edilebilir.
});

app.MapReverseProxy();

await app.RunAsync();
