using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Identity.Domain.Entities;

namespace ShuffleSeries.Identity.Application.Common.Interfaces;

public interface ITokenService
{
    TimeSpan AccessTokenLifetime { get; }
    TimeSpan RefreshTokenLifetime { get; }
    TimeSpan GuestRefreshTokenLifetime { get; }
    Task<TokenResponse> GenerateTokensAsync(User user, IReadOnlyList<string> roles, IReadOnlyList<string> permissions, CancellationToken cancellationToken = default);
    string HashToken(string token);
    string GenerateRefreshToken();
    (string? JwtId, DateTime? ExpiresAtUtc) ExtractTokenInfo(string token, Guid? expectedUserId = null);
}
