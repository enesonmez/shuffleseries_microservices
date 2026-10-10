using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Repositories;

namespace ShuffleSeries.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public void Add(User user) => _context.Users.Add(user);
    public void Update(User user) => _context.Users.Update(user);
    public void Delete(User user) => _context.Users.Remove(user);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .Include(u => u.UserPermissions)
            .Include(u => u.UserLogins)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .Include(u => u.UserPermissions)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return await _context.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .Include(u => u.UserPermissions)
            .Include(u => u.UserLogins)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default) =>
        await _context.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .Include(u => u.UserPermissions)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.TokenHash == refreshTokenHash), cancellationToken);

    public async Task<User?> GetByLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default) =>
        await _context.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .Include(u => u.UserPermissions)
            .Include(u => u.UserLogins)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.UserLogins.Any(ul => ul.Provider == provider && ul.ProviderKey == providerKey), cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return await _context.Users
            .AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }
}
