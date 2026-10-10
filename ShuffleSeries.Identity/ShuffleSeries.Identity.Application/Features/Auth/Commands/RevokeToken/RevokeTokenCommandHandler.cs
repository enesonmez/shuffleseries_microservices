using MediatR;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.RevokeToken;

internal sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, bool>
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
        var targetTtl = TimeSpan.FromMinutes(60);

        if (string.IsNullOrEmpty(targetJti) && !string.IsNullOrEmpty(request.AccessToken))
        {
            var (parsedJti, parsedExp) = _tokenService.ExtractTokenInfo(request.AccessToken);
            targetJti = parsedJti;
            if (parsedExp.HasValue)
            {
                var remaining = parsedExp.Value - now;
                if (remaining > TimeSpan.Zero)
                {
                    targetTtl = remaining;
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
