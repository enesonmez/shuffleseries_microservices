using FluentValidation;
using MediatR;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.DeleteAccount;

public record DeleteAccountCommand(Guid UserId) : IRequest<bool>;

public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}

internal sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenBlacklistService? _blacklistService;

    public DeleteAccountCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ITokenBlacklistService? blacklistService = null)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _blacklistService = blacklistService;
    }

    public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException("User account not found.");
        }

        user.DeleteAccount();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (_blacklistService is not null)
        {
            await _blacklistService.BlacklistUserTokensAsync(user.Id, TimeSpan.FromHours(1), cancellationToken);
        }

        return true;
    }
}
