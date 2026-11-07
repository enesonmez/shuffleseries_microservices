using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.DeleteAccount;

public sealed class DeleteAccountCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ITokenService? _tokenService;
    private readonly ITokenBlacklistService? _blacklistService;

    public DeleteAccountCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ITokenBlacklistService? blacklistService = null,
        ITokenService? tokenService = null)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _blacklistService = blacklistService;
        _tokenService = tokenService;
    }

    public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException("User account not found.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        user.DeleteAccount(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _userRepository.ScrubRefreshTokenIpsByUserIdAsync(user.Id, cancellationToken);

        if (_blacklistService is not null)
        {
            var ttl = _tokenService is not null && _tokenService.AccessTokenLifetime > TimeSpan.Zero
                ? _tokenService.AccessTokenLifetime
                : TimeSpan.FromHours(1);
            await _blacklistService.BlacklistUserTokensAsync(user.Id, ttl, cancellationToken);
        }

        return true;
    }
}
