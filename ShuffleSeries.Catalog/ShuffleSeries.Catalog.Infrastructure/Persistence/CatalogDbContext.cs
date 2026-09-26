using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Outbox;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure.Extensions;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext : DbContext, IUnitOfWork
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Series> Series => Set<Series>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        modelBuilder.ApplySoftDeleteQueryFilters();
    }
}