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
using ShuffleSeries.ShuffleEngine.Api.Endpoints;
using ShuffleSeries.ShuffleEngine.Application;
using ShuffleSeries.ShuffleEngine.Infrastructure;
using ShuffleSeries.ShuffleEngine.Infrastructure.Persistence;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog("ShuffleEngine.Api");
builder.Configuration.AddVault("shuffleengine");

builder.Host.UseWolverine(opts =>
{
    opts.AddShuffleEngineApplicationMessaging();
});
builder.Host.ConfigureServices((context, services) =>
{
    services.ConfigureWolverine(opts => opts.AddShuffleEngineMessagingTransport(context.Configuration));
});

builder.Services.AddSharedCorrelation();
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "ShuffleEngine.Api");
builder.Services.AddSharedHealthChecks();

builder.Services.AddSharedSwagger(options =>
{
    options.Title = "ShuffleSeries Shuffle Engine API";
    options.Version = "v1";
    options.Description = "Shuffle Engine Bounded Context - High-Performance Redis Pool, Event Synchronization & Recommendation Core";
    options.IncludeJwtSecurity = true;
});

// Edge (Gateway) enforces Redis token revocation centrally; disable duplicate downstream check
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
{
    app.UseSharedSwagger();
    await app.ApplyDatabaseMigrationsAsync<ShuffleEngineDbContext>(
        (context, _) => ShuffleEngineDbSeeder.SeedAsync(context));
}

app.MapShuffleAdminEndpoints();

await app.RunAsync();

#pragma warning disable ASP0027
public partial class Program
{
    protected Program()
    {
    }
}
#pragma warning restore ASP0027
