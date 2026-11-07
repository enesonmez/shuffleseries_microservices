using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Common.Services;

internal sealed class AuthResponseService : IAuthResponseService
{
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionResolver _permissionResolver;
    private readonly TimeProvider _timeProvider;

    public AuthResponseService(
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IPermissionResolver permissionResolver,
        TimeProvider timeProvider)
    {
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _permissionResolver = permissionResolver;
        _timeProvider = timeProvider;
    }

    public async Task<AuthResponse> CreateAuthResponseAsync(
        User user,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var hashedRefreshToken = _tokenService.HashToken(rawRefreshToken);

        var lifetime = user.IsGuest
            ? _tokenService.GuestRefreshTokenLifetime
            : _tokenService.RefreshTokenLifetime;

        var expiresAt = _timeProvider.GetUtcNow().Add(lifetime).UtcDateTime;
        user.AddRefreshToken(hashedRefreshToken, expiresAt, ipAddress);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var (roles, permissions) = await _permissionResolver.ResolveEffectivePermissionsAsync(user, cancellationToken);
        var tokenResponse = await _tokenService.GenerateTokensAsync(user, roles, permissions, cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Email,
            user.IsGuest,
            roles,
            permissions,
            tokenResponse.AccessToken,
            rawRefreshToken,
            tokenResponse.ExpiresIn
        );
    }
}
