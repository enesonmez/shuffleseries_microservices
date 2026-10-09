using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using ShuffleSeries.Identity.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ShuffleSeries.Identity.IntegrationTests.Infrastructure;

public class IdentityApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    static IdentityApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Secret", "SuperSecretSecureKeyForIdentityIntegrationTesting2026!!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "ShuffleSeries.Identity");
        Environment.SetEnvironmentVariable("Jwt__Audience", "ShuffleSeries.Clients");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("Vault__Enabled", "false");
        Environment.SetEnvironmentVariable("Vault__Optional", "true");
    }

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("shuffleseries_identity_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private DbConnection _dbConnection = default!;
    private Respawner _respawner = default!;

    public async Task InitializeAsync()
    {
        // 1. PostgreSQL Testcontainer'ını başlat
        await _dbContainer.StartAsync();

        // 2. DbConnection'ı aç ve Respawn'ı yapılandır
        _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        // 3. WebApplicationFactory ilk başlatıldığında EF migration'ları ve seed verileri uygulanır
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync();

        // 4. Respawner nesnesini başlat (migration ve seed tablolarını sıfırlama dışında tutabiliriz veya migration seed'i resetten sonra çalıştırabiliriz)
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // HashiCorp Vault devredışı ve PostgreSQL connection string Testcontainers'a yönlendirilir
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _dbContainer.GetConnectionString(),
                ["Vault:Enabled"] = "false",
                ["Vault:Optional"] = "true",
                ["Jwt:Secret"] = "SuperSecretSecureKeyForIdentityIntegrationTesting2026!!",
                ["Jwt:Issuer"] = "ShuffleSeries.Identity",
                ["Jwt:Audience"] = "ShuffleSeries.Clients",
                ["Jwt:ExpirationMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    if (context.Exception is not null)
                    {
                        Console.WriteLine($"[TEST_DEBUG_EXCEPTION] {context.Exception}");
                    }
                };
            });

            services.PostConfigure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
            {
                var brokerChecks = options.Registrations
                    .Where(r => r.Name.Contains("masstransit", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var check in brokerChecks)
                {
                    options.Registrations.Remove(check);
                }
            });
        });
    }

    public Task ResetDatabaseAsync() => _respawner.ResetAsync(_dbConnection);

    public new async Task DisposeAsync()
    {
        if (_dbConnection != default!)
        {
            await _dbConnection.CloseAsync();
            await _dbConnection.DisposeAsync();
        }

        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
