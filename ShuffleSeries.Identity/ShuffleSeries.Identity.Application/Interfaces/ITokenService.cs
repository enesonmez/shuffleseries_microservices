using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Domain.Entities;

namespace ShuffleSeries.Identity.Application.Interfaces;

public interface ITokenService
{
    Task<TokenResponse> GenerateTokensAsync(User user, IReadOnlyList<string> roles, IReadOnlyList<string> permissions, CancellationToken cancellationToken = default);
    string HashToken(string rawToken);
    string GenerateRefreshToken();
    (string? JwtId, DateTime? ExpiresAtUtc) ExtractTokenInfo(string accessToken);
}
