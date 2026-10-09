using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Domain.Repositories;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Role?> GetDefaultRoleAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetRolesWithPermissionsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);
}
