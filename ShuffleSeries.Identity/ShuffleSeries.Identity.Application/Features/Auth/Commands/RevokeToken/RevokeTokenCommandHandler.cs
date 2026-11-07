using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.RevokeToken;

public sealed class RevokeTokenCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;
    private readonly ITokenBlacklistService? _blacklistService;

    public RevokeTokenCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        TimeProvider timeProvider,
        ITokenBlacklistService? blacklistService = null)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
        _blacklistService = blacklistService;
    }

    public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var incomingHash = _tokenService.HashToken(request.RefreshToken);
        var user = await _userRepository.GetByRefreshTokenHashAsync(incomingHash, cancellationToken);

        if (user is null)
        {
            throw new TokenNotFoundException();
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        user.RevokeRefreshToken(incomingHash, revokedAtUtc: now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Redis Token Blacklist integration for Access Token (OWASP API4)
        var targetJti = request.JwtId;
        if (request.CallerUserId.HasValue && request.CallerUserId.Value != user.Id)
        {
            // Do not trust caller's JwtId if caller does not own the refresh token
            targetJti = null;
        }

        var targetTtl = _tokenService.AccessTokenLifetime > TimeSpan.Zero
            ? _tokenService.AccessTokenLifetime
            : TimeSpan.FromMinutes(60);

        if (string.IsNullOrEmpty(targetJti) && !string.IsNullOrEmpty(request.AccessToken))
        {
            var (parsedJti, parsedExp) = _tokenService.ExtractTokenInfo(request.AccessToken, user.Id);
            targetJti = parsedJti;
            if (parsedExp.HasValue)
            {
                var remaining = parsedExp.Value - now;
                var maxTtl = targetTtl;
                if (remaining > TimeSpan.Zero)
                {
                    targetTtl = remaining > maxTtl ? maxTtl : remaining;
                }
            }
        }

        if (!string.IsNullOrEmpty(targetJti) && _blacklistService is not null)
        {
            await _blacklistService.BlacklistTokenAsync(targetJti, targetTtl, "logout", cancellationToken);
        }

        return true;
    }
}
