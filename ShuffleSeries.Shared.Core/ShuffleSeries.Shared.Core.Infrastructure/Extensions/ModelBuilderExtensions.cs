using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Infrastructure.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType is null || entityType.IsOwned() || entityType.BaseType is not null)
            {
                continue;
            }

            if (typeof(IHardDeletable).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Ignore(nameof(IHardDeletable.IsHardDeleteRequested));
            }

            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var notExpression = Expression.Not(property);
                var lambda = Expression.Lambda(notExpression, parameter);

                entityType.SetQueryFilter(lambda);
            }
        }
    }
}
