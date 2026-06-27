using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Repositories;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext : DbContext, IUnitOfWork
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) {}
    
    public DbSet<Series> Series => Set<Series>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}