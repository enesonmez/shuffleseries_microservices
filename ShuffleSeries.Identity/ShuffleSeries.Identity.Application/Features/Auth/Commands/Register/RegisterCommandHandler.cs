using MediatR;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.Register;

internal sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthResponseService _authResponseService;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IAuthResponseService authResponseService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _authResponseService = authResponseService;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new EmailAlreadyInUseException(request.Email);
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = User.CreateStandard(email, passwordHash);

        var defaultRole = await _roleRepository.GetDefaultRoleAsync(cancellationToken)
                          ?? await _roleRepository.GetByNameAsync(SystemRoles.Standard, cancellationToken);

        if (defaultRole is not null)
        {
            user.AssignRole(defaultRole.Id);
        }

        _userRepository.Add(user);
        return await _authResponseService.CreateAuthResponseAsync(user, request.IpAddress, cancellationToken);
    }
}
