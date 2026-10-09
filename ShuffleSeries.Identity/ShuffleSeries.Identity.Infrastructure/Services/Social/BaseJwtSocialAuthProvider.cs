using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Infrastructure.Configuration;

namespace ShuffleSeries.Identity.Infrastructure.Services.Social;

/// <summary>
/// Base class for OpenID Connect / JWT-based social identity providers.
/// Implements JWKS caching, signature verification, and configurable bypass modes.
/// </summary>
internal abstract class BaseJwtSocialAuthProvider : ISocialAuthProvider
{
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly HttpClient _httpClient;
    private readonly ExternalAuthOptions _options;
    private (JsonWebKeySet Keys, DateTime ExpiresAtUtc)? _cachedJwks;
    private readonly SemaphoreSlim _jwksLock = new(1, 1);

    protected BaseJwtSocialAuthProvider(HttpClient httpClient, IOptions<ExternalAuthOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public abstract string Provider { get; }
    protected abstract string JwksUri { get; }
    protected abstract string[] ValidIssuers { get; }
    protected abstract string? ClientId { get; }

    public async Task<ExternalUserPrincipal?> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        try
        {
            if (_options.ValidateSignatures)
            {
                return await ValidateCryptographicallyAsync(idToken, cancellationToken);
            }

            return ValidateInBypassMode(idToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task<ExternalUserPrincipal?> ValidateCryptographicallyAsync(string idToken, CancellationToken cancellationToken)
    {
        var keys = await GetSigningKeysAsync(cancellationToken);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = ValidIssuers,
            ValidateAudience = !string.IsNullOrWhiteSpace(ClientId),
            ValidAudience = ClientId,
            ValidateLifetime = true,
            IssuerSigningKeys = keys,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        var principal = _handler.ValidateToken(idToken, validationParameters, out _);
        var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? principal.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(sub))
        {
            return null;
        }

        var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? principal.FindFirst("email")?.Value;

        var name = principal.FindFirst(JwtRegisteredClaimNames.Name)?.Value
                   ?? principal.FindFirst("name")?.Value;

        return new ExternalUserPrincipal(Provider, sub, email, name);
    }

    private ExternalUserPrincipal? ValidateInBypassMode(string idToken)
    {
        if (_handler.CanReadToken(idToken))
        {
            var jwt = _handler.ReadJwtToken(idToken);
            var sub = jwt.Subject ?? jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (string.IsNullOrWhiteSpace(sub))
            {
                return null;
            }

            var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

            return new ExternalUserPrincipal(Provider, sub, email, name);
        }

        // Support mocked test tokens:
        // 1. "mock_sub:email@domain.com"
        // 2. "mock:sub:email@domain.com"
        // 3. "mock_sub"
        if (idToken.StartsWith("mock", StringComparison.OrdinalIgnoreCase))
        {
            var parts = idToken.Split(':');
            string subjectId;
            string email;

            if (parts.Length >= 3)
            {
                subjectId = parts[1];
                email = parts[2];
            }
            else if (parts.Length == 2)
            {
                subjectId = parts[0].StartsWith("mock_", StringComparison.OrdinalIgnoreCase)
                    ? parts[0][5..]
                    : parts[0];
                email = parts[1];
            }
            else
            {
                subjectId = idToken.StartsWith("mock_", StringComparison.OrdinalIgnoreCase)
                    ? idToken[5..]
                    : idToken;
                email = $"{subjectId}@{Provider.ToLowerInvariant()}.com";
            }

            return new ExternalUserPrincipal(Provider, subjectId, email, "Test User");
        }

        return null;
    }

    private async Task<ICollection<SecurityKey>> GetSigningKeysAsync(CancellationToken cancellationToken)
    {
        if (_cachedJwks is { } cached && cached.ExpiresAtUtc > DateTime.UtcNow)
        {
            return cached.Keys.GetSigningKeys();
        }

        await _jwksLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedJwks is { } lockedCached && lockedCached.ExpiresAtUtc > DateTime.UtcNow)
            {
                return lockedCached.Keys.GetSigningKeys();
            }

            var json = await _httpClient.GetStringAsync(JwksUri, cancellationToken);
            var jwks = new JsonWebKeySet(json);
            _cachedJwks = (jwks, DateTime.UtcNow.AddHours(12));
            return jwks.GetSigningKeys();
        }
        finally
        {
            _jwksLock.Release();
        }
    }
}
