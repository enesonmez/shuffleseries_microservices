using Scalar.AspNetCore;
using ShuffleSeries.Catalog.Api.Endpoints;
using ShuffleSeries.Catalog.Api.Extensions;
using ShuffleSeries.Catalog.Application;
using ShuffleSeries.Catalog.Infrastructure;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Vault;
using ShuffleSeries.Shared.Core.Web;

var builder = WebApplication.CreateBuilder(args);

// Configure HashiCorp Vault Secrets Management
builder.Configuration.AddVault("catalog");

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSharedExceptionHandling();

var app = builder.Build();

app.UseSharedExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapSeriesEndpoints();

await app.ApplyMigrationsAsync();

app.Run();
