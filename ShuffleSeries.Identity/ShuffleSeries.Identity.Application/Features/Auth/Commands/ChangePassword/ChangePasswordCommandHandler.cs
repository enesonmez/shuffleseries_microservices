using MediatR;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.ChangePassword;

internal sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;
    private readonly ITokenService? _tokenService;
    private readonly ITokenBlacklistService? _blacklistService;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider,
        ITokenBlacklistService? blacklistService = null,
        ITokenService? tokenService = null)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _blacklistService = blacklistService;
        _tokenService = tokenService;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException();
        }

        if (user.PasswordHash is null || !_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidCurrentPasswordException();
        }

        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        user.UpdatePassword(newPasswordHash, revokedAtUtc: now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
