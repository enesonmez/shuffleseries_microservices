using MediatR;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.RevokeToken;

internal sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public RevokeTokenCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
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

        return true;
    }
}
