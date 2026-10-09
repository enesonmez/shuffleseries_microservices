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

        // 1. Gather all permissions granted via Roles
        var effectivePermissionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            foreach (var rp in role.RolePermissions)
            {
                if (rp.Permission is not null && !string.IsNullOrWhiteSpace(rp.Permission.Code))
                {
                    effectivePermissionCodes.Add(rp.Permission.Code);
                }
            }
        }

        // 2. Fetch custom UserPermission overrides
        var customPermissionIds = user.UserPermissions.Select(up => up.PermissionId).ToList();
        if (customPermissionIds.Count != 0)
        {
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
                    // Direct grant beyond roles
                    effectivePermissionCodes.Add(code);
                }
                else
                {
                    // Explicit revoke / exclusion
                    effectivePermissionCodes.Remove(code);
                }
            }
        }

        return (roleNames, effectivePermissionCodes.OrderBy(p => p).ToList());
    }
}
