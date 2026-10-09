using MediatR;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserResponse>;

internal sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPermissionResolver _permissionResolver;

    public GetCurrentUserQueryHandler(
        IUserRepository userRepository,
        IPermissionResolver permissionResolver)
    {
        _userRepository = userRepository;
        _permissionResolver = permissionResolver;
    }

    public async Task<CurrentUserResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithDetailsAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException();
        }

        var (roles, permissions) = await _permissionResolver.ResolveEffectivePermissionsAsync(user, cancellationToken);

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.IsGuest,
            user.Status.ToString(),
            roles,
            permissions
        );
    }
}
