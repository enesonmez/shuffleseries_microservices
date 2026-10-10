using MediatR;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Domain.Enums;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.Login;

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IPermissionResolver _permissionResolver;
    private readonly TimeProvider _timeProvider;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IPermissionResolver permissionResolver,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _permissionResolver = permissionResolver;
        _timeProvider = timeProvider;
    }

    private const string DummyVerificationHash = "AQIDBAUGBwgJCgsMDQ4PEA==:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || user.PasswordHash is null || user.Status != UserStatus.Active)
        {
            _passwordHasher.VerifyPassword(request.Password, DummyVerificationHash);
            throw new InvalidCredentialsException();
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var hashedRefreshToken = _tokenService.HashToken(rawRefreshToken);
        var expiresAt = _timeProvider.GetUtcNow().AddDays(14).UtcDateTime;
        user.AddRefreshToken(hashedRefreshToken, expiresAt, request.IpAddress);

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
