using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Repositories;

namespace ShuffleSeries.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;
    private readonly TimeProvider _timeProvider;

    public UserRepository(IdentityDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public void Add(User user) => _context.Users.Add(user);
    public void Update(User user)
    {
        if (_context.Entry(user).State == EntityState.Detached)
        {
            _context.Users.Update(user);
        }
    }
    public void Delete(User user) => _context.Users.Remove(user);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        return await _context.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .Include(u => u.UserPermissions)
            .Include(u => u.UserLogins)
            .Include(u => u.RefreshTokens.Where(rt => rt.RevokedAtUtc == null && rt.ExpiresAtUtc > now))
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Users
            .AsNoTracking()
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
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }

    public async Task<User?> GetByEmailWithLoginsAsync(string email, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var normalized = email.Trim().ToUpperInvariant();
        return await _context.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .Include(u => u.UserPermissions)
            .Include(u => u.UserLogins)
            .Include(u => u.RefreshTokens.Where(rt => rt.RevokedAtUtc == null && rt.ExpiresAtUtc > now))
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        return await _context.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .Include(u => u.UserPermissions)
            .Include(u => u.RefreshTokens.Where(rt => rt.TokenHash == refreshTokenHash || (rt.RevokedAtUtc == null && rt.ExpiresAtUtc > now)))
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.TokenHash == refreshTokenHash), cancellationToken);
    }

    public async Task<User?> GetByLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default) =>
        await _context.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .Include(u => u.UserPermissions)
            .Include(u => u.UserLogins)
            .FirstOrDefaultAsync(u => u.UserLogins.Any(ul => ul.Provider == provider && ul.ProviderKey == providerKey), cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return await _context.Users
            .AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }

    public async Task ScrubRefreshTokenIpsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.CreatedByIp != null)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.CreatedByIp, (string?)null), cancellationToken);
    }
}
