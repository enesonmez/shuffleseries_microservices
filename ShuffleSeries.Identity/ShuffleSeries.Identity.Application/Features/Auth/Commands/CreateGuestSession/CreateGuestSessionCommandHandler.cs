using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.CreateGuestSession;

public sealed class CreateGuestSessionCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAuthResponseService _authResponseService;

    public CreateGuestSessionCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IAuthResponseService authResponseService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _authResponseService = authResponseService;
    }

    public async Task<AuthResponse> Handle(CreateGuestSessionCommand request, CancellationToken cancellationToken)
    {
        var user = User.CreateGuest();

        var guestRole = await _roleRepository.GetByNameAsync(SystemRoles.Guest, cancellationToken);
        if (guestRole is not null)
        {
            user.AssignRole(guestRole.Id);
        }

        _userRepository.Add(user);
        return await _authResponseService.CreateAuthResponseAsync(user, request.IpAddress, cancellationToken);
    }
}
