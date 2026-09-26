using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Infrastructure.Interceptors;

public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplySoftDelete(DbContext? context)
    {
        if (context is null || HardDeleteScope.IsActive)
        {
            return;
        }

        var entries = context.ChangeTracker
            .Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.Entity is IHardDeletable { IsHardDeleteRequested: true })
            {
                continue;
            }

            entry.State = EntityState.Modified;

            // Eğer domain seviyesinde (örn. entity.Delete("user")) zaten silinmişse
            // atanmış olan DeletedBy ve DeletedAtUtc değerlerini koru, ezme!
            if (!entry.Entity.IsDeleted)
            {
                entry.Entity.SoftDelete();
            }

            entry.Property(nameof(ISoftDeletable.IsDeleted)).IsModified = true;
            entry.Property(nameof(ISoftDeletable.DeletedAtUtc)).IsModified = true;
            if (entry.Entity.DeletedBy is not null)
            {
                entry.Property(nameof(ISoftDeletable.DeletedBy)).IsModified = true;
            }
        }
    }
}
