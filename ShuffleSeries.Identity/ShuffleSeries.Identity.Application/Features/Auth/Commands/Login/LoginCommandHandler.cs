using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Identity.Domain.Enums;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthResponseService _authResponseService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthResponseService authResponseService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _authResponseService = authResponseService;
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

        return await _authResponseService.CreateAuthResponseAsync(user, request.IpAddress, cancellationToken);
    }
}
