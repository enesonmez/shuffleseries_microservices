using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Shared.Core.Domain.Inbox;
using ShuffleSeries.Shared.Core.Domain.Outbox;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure.Extensions;
using ShuffleSeries.Shared.Core.Infrastructure.Persistence.Configurations;

namespace ShuffleSeries.ShuffleEngine.Infrastructure.Persistence;

public class ShuffleEngineDbContext(DbContextOptions<ShuffleEngineDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplySoftDeleteQueryFilters();
        base.OnModelCreating(modelBuilder);
    }
}
