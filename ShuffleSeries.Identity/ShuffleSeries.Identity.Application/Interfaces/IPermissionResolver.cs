using ShuffleSeries.Identity.Domain.Entities;

namespace ShuffleSeries.Identity.Application.Interfaces;

public interface IPermissionResolver
{
    Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> ResolveEffectivePermissionsAsync(User user, CancellationToken cancellationToken = default);
}
