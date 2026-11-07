using MediatR;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Enums;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.SocialLogin;

internal sealed class SocialLoginCommandHandler : IRequestHandler<SocialLoginCommand, AuthResponse>
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

        var user = await _userRepository.GetByLoginAsync(request.Provider, externalPrincipal.SubjectId, cancellationToken);
        var shouldRevokePriorTokens = false;
        DateTimeOffset? preLinkRevokedCutoff = null;

        if (user is not null)
        {
            if (user.Status != UserStatus.Active)
            {
                throw new InvalidCredentialsException();
            }
        }
        else
        {
            // Check if account with same email already exists
            if (!string.IsNullOrWhiteSpace(externalPrincipal.Email))
            {
                user = await _userRepository.GetByEmailWithLoginsAsync(externalPrincipal.Email.Trim().ToLowerInvariant(), cancellationToken);
            }

            if (user is not null)
            {
                if (user.Status != UserStatus.Active)
                {
                    throw new InvalidCredentialsException();
                }

                var now = _timeProvider.GetUtcNow().UtcDateTime;

                // Pre-Account Takeover Savunması (Seçenek A):
                // Doğrulanmış Google/Apple hesabı ilk kez bağlandığında, önceden var olabilecek
                // yerel şifreyi temizle ve önceki tüm oturumları geçersiz kıl.
                var isFirstSocialLink = user.UserLogins.Count == 0;
                if (isFirstSocialLink && user.PasswordHash is not null)
                {
                    user.ClearUnverifiedPasswordOnSocialLink();
                    user.RevokeAllRefreshTokens(revokedAtUtc: now);
                    shouldRevokePriorTokens = true;
                    preLinkRevokedCutoff = _timeProvider.GetUtcNow().AddMilliseconds(-1);
                }

                // Account linking
                user.AddLogin(request.Provider, externalPrincipal.SubjectId, externalPrincipal.Email, linkedAtUtc: now);
            }
            else
            {
                // Create brand new social user
                var fallbackEmail = !string.IsNullOrWhiteSpace(externalPrincipal.Email)
                    ? externalPrincipal.Email
                    : $"{request.Provider.ToLowerInvariant()}_{externalPrincipal.SubjectId}@shuffleseries.internal";

                user = User.CreateSocial(
                    fallbackEmail,
                    request.Provider,
                    externalPrincipal.SubjectId,
                    _timeProvider.GetUtcNow().UtcDateTime);

                var defaultRole = await _roleRepository.GetDefaultRoleAsync(cancellationToken)
                                  ?? await _roleRepository.GetByNameAsync(SystemRoles.Standard, cancellationToken);

                if (defaultRole is not null)
                {
                    user.AssignRole(defaultRole.Id);
                }

                _userRepository.Add(user);
            }
        }

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
}
