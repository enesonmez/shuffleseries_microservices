using MediatR;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.CreateGuestSession;

public record CreateGuestSessionCommand(string? IpAddress = null) : IRequest<AuthResponse>;

internal sealed class CreateGuestSessionCommandHandler : IRequestHandler<CreateGuestSessionCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IPermissionResolver _permissionResolver;
    private readonly TimeProvider _timeProvider;

    public CreateGuestSessionCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IPermissionResolver permissionResolver,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _permissionResolver = permissionResolver;
        _timeProvider = timeProvider;
    }

    public async Task<AuthResponse> Handle(CreateGuestSessionCommand request, CancellationToken cancellationToken)
    {
        var user = User.CreateGuest();

        var guestRole = await _roleRepository.GetByNameAsync(SystemRoles.Guest, cancellationToken);
        if (guestRole is not null)
        {
            user.AssignRole(guestRole.Id);
        }

        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var hashedRefreshToken = _tokenService.HashToken(rawRefreshToken);
        var expiresAt = _timeProvider.GetUtcNow().AddDays(7).UtcDateTime;
        user.AddRefreshToken(hashedRefreshToken, expiresAt, request.IpAddress);

        _userRepository.Add(user);
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
