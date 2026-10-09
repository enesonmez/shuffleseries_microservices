using ShuffleSeries.Identity.Api.Endpoints;
using ShuffleSeries.Identity.Api.Extensions;
using ShuffleSeries.Identity.Application;
using ShuffleSeries.Identity.Infrastructure;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Vault;
using ShuffleSeries.Shared.Core.Web;
using ShuffleSeries.Shared.Core.Web.Authentication;
using ShuffleSeries.Shared.Core.Web.Correlation;
using ShuffleSeries.Shared.Core.Web.Cors;
using ShuffleSeries.Shared.Core.Web.Health;
using ShuffleSeries.Shared.Core.Web.Logging;
using ShuffleSeries.Shared.Core.Web.Observability;
using ShuffleSeries.Shared.Core.Web.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with extensible sinks (Console, File, PostgreSQL)
builder.Host.UseSharedSerilog("Identity.Api");

// Configure HashiCorp Vault Secrets Management
builder.Configuration.AddVault("identity");

// Add Distributed Correlation ID Tracking
builder.Services.AddSharedCorrelation();

// Add OpenTelemetry Tracing and Metrics
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Identity.Api");

// Add Standard Health Checks (Liveness & Readiness)
builder.Services.AddSharedHealthChecks();

// Add Swagger & OpenAPI Documentation
builder.Services.AddSharedSwagger(options =>
{
    options.Title = "ShuffleSeries Identity API";
    options.Version = "v1";
    options.Description = "Identity Bounded Context - Authentication, JWT Token Management, Guest Sessions, and Social Login";
    options.IncludeJwtSecurity = true;
});

// Add Shared JWT Authentication & Authorization
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Centralized CORS Policy
builder.Services.AddSharedCors(builder.Configuration);

builder.Services.AddSharedExceptionHandling();

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
}

app.MapAuthEndpoints();

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
