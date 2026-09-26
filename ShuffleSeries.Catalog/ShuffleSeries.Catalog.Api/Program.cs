using ShuffleSeries.Catalog.Api.Endpoints;
using ShuffleSeries.Catalog.Api.Extensions;
using ShuffleSeries.Catalog.Application;
using ShuffleSeries.Catalog.Infrastructure;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Vault;
using ShuffleSeries.Shared.Core.Web;
using ShuffleSeries.Shared.Core.Web.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Configure HashiCorp Vault Secrets Management
builder.Configuration.AddVault("catalog");

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

builder.Services.AddSharedExceptionHandling();

var app = builder.Build();

app.UseSharedExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSharedSwagger();
}

app.UseHttpsRedirection();

app.MapSeriesEndpoints();

await app.ApplyMigrationsAsync();

app.Run();
