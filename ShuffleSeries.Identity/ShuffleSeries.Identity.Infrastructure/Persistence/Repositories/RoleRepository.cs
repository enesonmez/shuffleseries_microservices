using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Repositories;

namespace ShuffleSeries.Identity.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _context;

    public RoleRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToUpperInvariant();
        return await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.NormalizedName == normalized, cancellationToken);
    }

    public async Task<Role?> GetDefaultRoleAsync(CancellationToken cancellationToken = default) =>
        await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.IsDefault, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetRolesWithPermissionsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var roleIdList = roleIds.ToList();
        return await _context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .Where(r => roleIdList.Contains(r.Id))
            .ToListAsync(cancellationToken);
    }

    public void Add(Role role) => _context.Roles.Add(role);

    public void Update(Role role) => _context.Roles.Update(role);

    public void Delete(Role role) => _context.Roles.Remove(role);
}
