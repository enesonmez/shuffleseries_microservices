using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.ConvertGuest;

public sealed class ConvertGuestCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthResponseService _authResponseService;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;
    private readonly ITokenBlacklistService? _blacklistService;

    public ConvertGuestCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IAuthResponseService authResponseService,
        ITokenService tokenService,
        TimeProvider timeProvider,
        ITokenBlacklistService? blacklistService = null)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _authResponseService = authResponseService;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
        _blacklistService = blacklistService;
    }

    public async Task<AuthResponse> Handle(ConvertGuestCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException("Guest user account not found.");
        }

        if (!user.IsGuest)
        {
            throw new UserAlreadyRegisteredException();
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new EmailAlreadyInUseException(request.Email);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        user.ConvertFromGuest(email, passwordHash, now);

        // Transition from Guest role to Standard role
        var guestRole = await _roleRepository.GetByNameAsync(SystemRoles.Guest, cancellationToken);
        if (guestRole is not null)
        {
            user.RemoveRole(guestRole.Id);
        }

        var standardRole = await _roleRepository.GetDefaultRoleAsync(cancellationToken)
                          ?? await _roleRepository.GetByNameAsync(SystemRoles.Standard, cancellationToken);
        if (standardRole is not null)
        {
            user.AssignRole(standardRole.Id);
        }

        if (!string.IsNullOrEmpty(request.CurrentJwtId) && _blacklistService is not null)
        {
            var ttl = _tokenService.AccessTokenLifetime > TimeSpan.Zero
                ? _tokenService.AccessTokenLifetime
                : TimeSpan.FromHours(1);
            await _blacklistService.BlacklistTokenAsync(request.CurrentJwtId, ttl, "guest-converted", cancellationToken);
        }

        return await _authResponseService.CreateAuthResponseAsync(user, request.IpAddress, cancellationToken);
    }
}
