using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Common.Models;
using ShuffleSeries.Identity.Infrastructure.Configuration;

namespace ShuffleSeries.Identity.Infrastructure.Services.Social;

/// <summary>
/// Base class for OpenID Connect / JWT-based social identity providers.
/// Implements JWKS caching, signature verification, and configurable bypass modes.
/// </summary>
internal abstract class BaseJwtSocialAuthProvider : ISocialAuthProvider, IDisposable
{
    private const string JwksHttpClientName = "OidcJwks";

    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExternalAuthOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly IHostEnvironment? _environment;
    private readonly ILogger? _logger;
    private (JsonWebKeySet Keys, DateTime ExpiresAtUtc)? _cachedJwks;
    private readonly SemaphoreSlim _jwksLock = new(1, 1);

    protected BaseJwtSocialAuthProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalAuthOptions> options,
        TimeProvider? timeProvider = null,
        IHostEnvironment? environment = null,
        ILogger? logger = null)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _environment = environment;
        _logger = logger;
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

        if (!_options.ValidateSignatures && !(_environment is not null && (_environment.IsDevelopment() || _environment.IsEnvironment("Testing"))))
        {
            throw new InvalidOperationException("External authentication signature validation bypass is forbidden outside development or testing environments.");
        }

        try
        {
            if (_options.ValidateSignatures)
            {
                return await ValidateCryptographicallyAsync(idToken, cancellationToken);
            }

            return ValidateInBypassMode(idToken);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (_logger?.IsEnabled(LogLevel.Warning) == true)
            {
                _logger.LogWarning(ex, "External authentication token validation failed for provider {Provider}.", Provider);
            }
            return null;
        }
    }

    private async Task<ExternalUserPrincipal?> ValidateCryptographicallyAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            return null; // Fail-closed when signatures are validated but ClientId is unconfigured
        }

        var keys = await GetSigningKeysAsync(cancellationToken);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = ValidIssuers,
            ValidateAudience = true,
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

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailVerifiedClaim = principal.FindFirst("email_verified")?.Value;
            if (string.IsNullOrWhiteSpace(emailVerifiedClaim) || !IsEmailVerified(emailVerifiedClaim))
            {
                return null;
            }
        }

        var name = principal.FindFirst(JwtRegisteredClaimNames.Name)?.Value
                   ?? principal.FindFirst("name")?.Value;

        return new ExternalUserPrincipal(Provider, sub, email, name);
    }

    private ExternalUserPrincipal? ValidateInBypassMode(string idToken)
    {
        if (_handler.CanReadToken(idToken))
        {
            return ValidateReadableJwtToken(idToken);
        }

        if (idToken.StartsWith("mock", StringComparison.OrdinalIgnoreCase))
        {
            return ParseMockTokenString(idToken);
        }

        return null;
    }

    private ExternalUserPrincipal? ValidateReadableJwtToken(string idToken)
    {
        var jwt = _handler.ReadJwtToken(idToken);
        var sub = jwt.Subject ?? jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (string.IsNullOrWhiteSpace(sub))
        {
            return null;
        }

        var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailVerifiedClaim = jwt.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value;
            if (string.IsNullOrWhiteSpace(emailVerifiedClaim) || !IsEmailVerified(emailVerifiedClaim))
            {
                return null;
            }
        }

        var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

        return new ExternalUserPrincipal(Provider, sub, email, name);
    }

    private static bool IsEmailVerified(string claimValue) =>
        (bool.TryParse(claimValue, out var verified) && verified)
        || string.Equals(claimValue, "true", StringComparison.OrdinalIgnoreCase);

    private ExternalUserPrincipal ParseMockTokenString(string idToken)
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

    private async Task<ICollection<SecurityKey>> GetSigningKeysAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (_cachedJwks is { } cached && cached.ExpiresAtUtc > now)
        {
            return cached.Keys.GetSigningKeys();
        }

        await _jwksLock.WaitAsync(cancellationToken);
        try
        {
            now = _timeProvider.GetUtcNow().UtcDateTime;
            if (_cachedJwks is { } lockedCached && lockedCached.ExpiresAtUtc > now)
            {
                return lockedCached.Keys.GetSigningKeys();
            }

            try
            {
                var client = _httpClientFactory.CreateClient(JwksHttpClientName);
                var json = await client.GetStringAsync(JwksUri, cancellationToken);
                var jwks = new JsonWebKeySet(json);
                _cachedJwks = (jwks, now.AddHours(12));
                return jwks.GetSigningKeys();
            }
            catch (Exception ex) when (_cachedJwks is { } stale && now < stale.ExpiresAtUtc.AddHours(1))
            {
                if (_logger?.IsEnabled(LogLevel.Warning) == true)
                {
                    _logger.LogWarning(ex, "[{Provider}] Failed to refresh JWKS from {JwksUri}. Returning stale cached signing keys.", Provider, JwksUri);
                }

                return stale.Keys.GetSigningKeys();
            }
        }
        finally
        {
            _jwksLock.Release();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _jwksLock.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
