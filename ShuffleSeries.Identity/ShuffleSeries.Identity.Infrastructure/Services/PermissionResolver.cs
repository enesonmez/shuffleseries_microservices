using Microsoft.Extensions.Caching.Memory;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Repositories;

namespace ShuffleSeries.Identity.Infrastructure.Services;

internal sealed class PermissionResolver : IPermissionResolver
{
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMemoryCache _cache;

    public PermissionResolver(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IMemoryCache cache)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _cache = cache;
    }

    public async Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> ResolveEffectivePermissionsAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).Distinct().ToList();
        var (roleNames, rolePermissions) = await GetCachedRoleDataAsync(roleIds, cancellationToken);

        var effectivePermissionCodes = new HashSet<string>(rolePermissions, StringComparer.OrdinalIgnoreCase);

        await ApplyUserPermissionOverridesAsync(user, effectivePermissionCodes, cancellationToken);

        return (roleNames, effectivePermissionCodes.OrderBy(p => p).ToList());
    }

    private async Task<(List<string> Names, List<string> Permissions)> GetCachedRoleDataAsync(
        List<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return ([], []);
        }

        var names = new List<string>(roleIds.Count);
        var permissions = new List<string>();
        var uncachedRoleIds = new List<Guid>();

        foreach (var roleId in roleIds)
        {
            if (_cache.TryGetValue($"rbac:role:{roleId}", out (string Name, List<string> Perms) cached))
            {
                names.Add(cached.Name);
                permissions.AddRange(cached.Perms);
            }
            else
            {
                uncachedRoleIds.Add(roleId);
            }
        }

        if (uncachedRoleIds.Count > 0)
        {
            var fetchedRoles = await _roleRepository.GetRolesWithPermissionsAsync(uncachedRoleIds, cancellationToken);
            foreach (var role in fetchedRoles)
            {
                var rolePerms = role.RolePermissions
                    .Where(rp => rp.Permission is not null && !string.IsNullOrWhiteSpace(rp.Permission.Code))
                    .Select(rp => rp.Permission!.Code)
                    .Distinct()
                    .ToList();

                _cache.Set($"rbac:role:{role.Id}", (role.Name, rolePerms), _cacheDuration);

                names.Add(role.Name);
                permissions.AddRange(rolePerms);
            }
        }

        return (names.Distinct().ToList(), permissions.Distinct().ToList());
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
