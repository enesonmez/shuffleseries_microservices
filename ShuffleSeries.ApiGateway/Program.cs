using ShuffleSeries.Shared.Core.Web.Cors;

var builder = WebApplication.CreateBuilder(args);

// Add Centralized CORS Policy
builder.Services.AddSharedCors(builder.Configuration);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseSharedCors();

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
