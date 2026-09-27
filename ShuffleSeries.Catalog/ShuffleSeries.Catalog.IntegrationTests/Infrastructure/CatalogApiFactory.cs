using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ShuffleSeries.Catalog.IntegrationTests.Infrastructure;

public class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("shuffleseries_catalog_test")
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

        // 3. WebApplicationFactory ilk başlatıldığında EF migration'ları uygulanır
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await dbContext.Database.MigrateAsync();

        // 4. Respawner nesnesini başlat (migration tablosunu sıfırlama dışında tut)
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
                ["MessageBroker:Host"] = "localhost"
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
