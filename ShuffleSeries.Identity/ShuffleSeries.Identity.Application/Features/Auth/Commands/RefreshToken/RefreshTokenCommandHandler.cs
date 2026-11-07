using MediatR;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Identity.Domain.Enums;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.RefreshToken;

internal sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IPermissionResolver _permissionResolver;
    private readonly TimeProvider _timeProvider;
    private readonly ITokenBlacklistService? _blacklistService;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IPermissionResolver permissionResolver,
        TimeProvider timeProvider,
        ITokenBlacklistService? blacklistService = null)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _permissionResolver = permissionResolver;
        _timeProvider = timeProvider;
        _blacklistService = blacklistService;
    }

    public async Task<TokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var incomingHash = _tokenService.HashToken(request.RefreshToken);
        var user = await _userRepository.GetByRefreshTokenHashAsync(incomingHash, cancellationToken);

        if (user is null || (user.Status != UserStatus.Active && user.Status != UserStatus.Guest))
        {
            throw new InvalidRefreshTokenException();
        }

        var currentToken = user.RefreshTokens.FirstOrDefault(rt => rt.TokenHash == incomingHash);
        if (currentToken is null)
        {
            throw new InvalidRefreshTokenException();
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // OWASP API4: Refresh Token Reuse Detection
        if (currentToken.IsRevoked)
        {
            user.RevokeAllRefreshTokens(revokedAtUtc: now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (_blacklistService is not null)
            {
                var ttl = _tokenService.AccessTokenLifetime > TimeSpan.Zero
                    ? _tokenService.AccessTokenLifetime
                    : TimeSpan.FromHours(1);
                await _blacklistService.BlacklistUserTokensAsync(user.Id, ttl, cancellationToken);
            }

            throw new TokenCompromisedException();
        }

        if (currentToken.IsExpiredAt(now))
        {
            throw new RefreshTokenExpiredException();
        }

        // Token Rotation
        var newRawRefreshToken = _tokenService.GenerateRefreshToken();
        var newHashedRefreshToken = _tokenService.HashToken(newRawRefreshToken);

        var tokenLifetime = user.IsGuest
            ? _tokenService.GuestRefreshTokenLifetime
            : _tokenService.RefreshTokenLifetime;
        currentToken.Revoke(newHashedRefreshToken, revokedAtUtc: now);
        user.AddRefreshToken(newHashedRefreshToken, now.Add(tokenLifetime), request.IpAddress);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var (roles, permissions) = await _permissionResolver.ResolveEffectivePermissionsAsync(user, cancellationToken);
        var tokenResponse = await _tokenService.GenerateTokensAsync(user, roles, permissions, cancellationToken);

        return new TokenResponse(
            tokenResponse.AccessToken,
            newRawRefreshToken,
            tokenResponse.ExpiresIn
        );
    }
}
