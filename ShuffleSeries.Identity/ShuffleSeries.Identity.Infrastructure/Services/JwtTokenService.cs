using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Identity.Domain.Entities;

namespace ShuffleSeries.Identity.Infrastructure.Services;

internal sealed class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly SigningCredentials _signingCredentials;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenService(IConfiguration configuration, TimeProvider timeProvider)
    {
        _configuration = configuration;
        _timeProvider = timeProvider;

        var secret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        if (Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must be at least 32 bytes (256 bits) for HMAC-SHA256.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        _issuer = _configuration["Jwt:Issuer"] ?? "ShuffleSeries.Identity";
        _audience = _configuration["Jwt:Audience"] ?? "ShuffleSeries.Microservices";
    }

    public TimeSpan AccessTokenLifetime
    {
        get
        {
            var expirationMinutes = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var exp) && exp > 0 ? exp : 60;
            return TimeSpan.FromMinutes(expirationMinutes);
        }
    }

    public TimeSpan RefreshTokenLifetime
    {
        get
        {
            var days = int.TryParse(_configuration["Jwt:RefreshTokenExpirationDays"], out var exp) && exp > 0 ? exp : 14;
            return TimeSpan.FromDays(days);
        }
    }

    public TimeSpan GuestRefreshTokenLifetime
    {
        get
        {
            var days = int.TryParse(_configuration["Jwt:GuestRefreshTokenExpirationDays"], out var exp) && exp > 0 ? exp : 30;
            return TimeSpan.FromDays(days);
        }
    }

    public Task<TokenResponse> GenerateTokensAsync(
        User user,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var nowUtc = now.UtcDateTime;
        var nowUnixSeconds = now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var nowUnixMs = now.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, nowUnixSeconds),
            new("iat_ms", nowUnixMs),
            new("is_guest", user.IsGuest.ToString().ToLowerInvariant()),
            new("security_stamp", user.SecurityStamp)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var expiresAt = now.Add(AccessTokenLifetime).UtcDateTime;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            IssuedAt = nowUtc,
            Expires = expiresAt,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = _signingCredentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = _tokenHandler.WriteToken(token);

        var response = new TokenResponse(
            accessToken,
            string.Empty,
            (int)AccessTokenLifetime.TotalSeconds
        );

        return Task.FromResult(response);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public string HashToken(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public (string? JwtId, DateTime? ExpiresAtUtc) ExtractTokenInfo(string accessToken, Guid? expectedUserId = null)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return (null, null);
        }

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingCredentials.Key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = false, // Allow logout/revocation of recently expired tokens
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(accessToken, validationParameters, out var validatedToken);
            if (validatedToken is not JwtSecurityToken jwt)
            {
                return (null, null);
            }

            if (expectedUserId.HasValue)
            {
                var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(sub, out var tokenUserId) || tokenUserId != expectedUserId.Value)
                {
                    return (null, null); // Mismatched user - prevents cross-user blacklisting
                }
            }

            return (jwt.Id, jwt.ValidTo);
        }
        catch
        {
            return (null, null);
        }
    }
}
