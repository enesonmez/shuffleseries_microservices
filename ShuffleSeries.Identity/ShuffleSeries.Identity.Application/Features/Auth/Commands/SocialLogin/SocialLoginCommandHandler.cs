using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Common.Models;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Enums;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.SocialLogin;

public sealed class SocialLoginCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IExternalAuthService _externalAuthService;
    private readonly IAuthResponseService _authResponseService;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;
    private readonly ITokenBlacklistService? _blacklistService;

    public SocialLoginCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IExternalAuthService externalAuthService,
        IAuthResponseService authResponseService,
        ITokenService tokenService,
        TimeProvider timeProvider,
        ITokenBlacklistService? blacklistService = null)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _externalAuthService = externalAuthService;
        _authResponseService = authResponseService;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
        _blacklistService = blacklistService;
    }

    public async Task<AuthResponse> Handle(SocialLoginCommand request, CancellationToken cancellationToken)
    {
        var externalPrincipal = await _externalAuthService.VerifyTokenAsync(request.Provider, request.IdToken, cancellationToken);
        if (externalPrincipal is null)
        {
            throw new InvalidExternalTokenException(request.Provider);
        }

        var (user, shouldRevokePriorTokens, preLinkRevokedCutoff) = await ResolveOrCreateSocialUserAsync(
            request.Provider,
            externalPrincipal,
            cancellationToken);

        var authResponse = await _authResponseService.CreateAuthResponseAsync(user, request.IpAddress, cancellationToken);

        if (shouldRevokePriorTokens && _blacklistService is not null)
        {
            var ttl = _tokenService.AccessTokenLifetime > TimeSpan.Zero
                ? _tokenService.AccessTokenLifetime
                : TimeSpan.FromHours(1);
            await _blacklistService.BlacklistUserTokensAsync(user.Id, ttl, preLinkRevokedCutoff, cancellationToken);
        }

        return authResponse;
    }

    private async Task<(User User, bool ShouldRevokePriorTokens, DateTimeOffset? PreLinkRevokedCutoff)> ResolveOrCreateSocialUserAsync(
        string provider,
        ExternalUserPrincipal externalPrincipal,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByLoginAsync(provider, externalPrincipal.SubjectId, cancellationToken);
        if (user is not null)
        {
            EnsureActiveUser(user);
            return (user, false, null);
        }

        if (!string.IsNullOrWhiteSpace(externalPrincipal.Email))
        {
            user = await _userRepository.GetByEmailWithLoginsAsync(
                externalPrincipal.Email.Trim().ToLowerInvariant(),
                cancellationToken);
        }

        if (user is not null)
        {
            return LinkExistingUser(user, provider, externalPrincipal);
        }

        var createdUser = await CreateNewSocialUserAsync(provider, externalPrincipal, cancellationToken);
        return (createdUser, false, null);
    }

    private (User User, bool ShouldRevokePriorTokens, DateTimeOffset? PreLinkRevokedCutoff) LinkExistingUser(
        User user,
        string provider,
        ExternalUserPrincipal externalPrincipal)
    {
        EnsureActiveUser(user);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var shouldRevokePriorTokens = false;
        DateTimeOffset? preLinkRevokedCutoff = null;

        // Pre-Account Takeover Savunması (Seçenek A):
        // Doğrulanmış Google/Apple hesabı ilk kez bağlandığında, önceden var olabilecek
        // yerel şifreyi temizle ve önceki tüm oturumları geçersiz kıl.
        if (user.UserLogins.Count == 0 && user.PasswordHash is not null)
        {
            user.ClearUnverifiedPasswordOnSocialLink();
            user.RevokeAllRefreshTokens(revokedAtUtc: now);
            shouldRevokePriorTokens = true;
            preLinkRevokedCutoff = _timeProvider.GetUtcNow().AddMilliseconds(-1);
        }

        user.AddLogin(provider, externalPrincipal.SubjectId, externalPrincipal.Email, linkedAtUtc: now);
        return (user, shouldRevokePriorTokens, preLinkRevokedCutoff);
    }

    private async Task<User> CreateNewSocialUserAsync(
        string provider,
        ExternalUserPrincipal externalPrincipal,
        CancellationToken cancellationToken)
    {
        var fallbackEmail = !string.IsNullOrWhiteSpace(externalPrincipal.Email)
            ? externalPrincipal.Email
            : $"{provider.ToLowerInvariant()}_{externalPrincipal.SubjectId}@shuffleseries.internal";

        var user = User.CreateSocial(
            fallbackEmail,
            provider,
            externalPrincipal.SubjectId,
            _timeProvider.GetUtcNow().UtcDateTime);

        var defaultRole = await _roleRepository.GetDefaultRoleAsync(cancellationToken)
                          ?? await _roleRepository.GetByNameAsync(SystemRoles.Standard, cancellationToken);

        if (defaultRole is not null)
        {
            user.AssignRole(defaultRole.Id);
        }

        _userRepository.Add(user);
        return user;
    }

    private static void EnsureActiveUser(User user)
    {
        if (user.Status != UserStatus.Active)
        {
            throw new InvalidCredentialsException();
        }
    }
}
