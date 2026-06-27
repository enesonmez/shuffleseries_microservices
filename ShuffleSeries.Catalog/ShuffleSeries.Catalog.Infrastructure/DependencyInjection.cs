using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Interface - Somut Sınıf eşleşmeleri
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());
        services.AddScoped<ISeriesRepository, SeriesRepository>();
    }
}