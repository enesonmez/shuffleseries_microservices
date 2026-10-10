using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Repositories;

namespace ShuffleSeries.Identity.Infrastructure.Services;

internal sealed class PermissionResolver : IPermissionResolver
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public PermissionResolver(IRoleRepository roleRepository, IPermissionRepository permissionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> ResolveEffectivePermissionsAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roles = await _roleRepository.GetRolesWithPermissionsAsync(roleIds, cancellationToken);
        var roleNames = roles.Select(r => r.Name).Distinct().ToList();

        var effectivePermissionCodes = GatherRolePermissions(roles);

        await ApplyUserPermissionOverridesAsync(user, effectivePermissionCodes, cancellationToken);

        return (roleNames, effectivePermissionCodes.OrderBy(p => p).ToList());
    }

    private static HashSet<string> GatherRolePermissions(IEnumerable<Role> roles)
    {
        var effectiveCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var codes = roles
            .SelectMany(r => r.RolePermissions)
            .Where(rp => rp.Permission is not null && !string.IsNullOrWhiteSpace(rp.Permission.Code))
            .Select(rp => rp.Permission!.Code);

        foreach (var code in codes)
        {
            effectiveCodes.Add(code);
        }

        return effectiveCodes;
    }

    private async Task ApplyUserPermissionOverridesAsync(
        User user,
        HashSet<string> effectivePermissionCodes,
        CancellationToken cancellationToken)
    {
        var customPermissionIds = user.UserPermissions.Select(up => up.PermissionId).ToList();
        if (customPermissionIds.Count == 0)
        {
            return;
        }

        var customPermissions = await _permissionRepository.GetByIdsAsync(customPermissionIds, cancellationToken);
        var permissionMap = customPermissions.ToDictionary(p => p.Id, p => p.Code);

        foreach (var up in user.UserPermissions)
        {
            if (!permissionMap.TryGetValue(up.PermissionId, out var code))
            {
                continue;
            }

            if (up.IsGranted)
            {
                effectivePermissionCodes.Add(code);
            }
            else
            {
                effectivePermissionCodes.Remove(code);
            }
        }
    }
}
