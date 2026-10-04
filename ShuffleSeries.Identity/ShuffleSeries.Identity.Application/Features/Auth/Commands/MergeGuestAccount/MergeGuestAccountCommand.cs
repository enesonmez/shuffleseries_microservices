using FluentValidation;
using MediatR;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.MergeGuestAccount;

public record MergeGuestAccountCommand(Guid GuestUserId, Guid TargetUserId) : IRequest<bool>;

public class MergeGuestAccountCommandValidator : AbstractValidator<MergeGuestAccountCommand>
{
    public MergeGuestAccountCommandValidator()
    {
        RuleFor(x => x.GuestUserId)
            .NotEmpty().WithMessage("Guest user ID is required.");

        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("Target user ID is required.")
            .NotEqual(x => x.GuestUserId).WithMessage("Guest user ID and target user ID must be different.");
    }
}

internal sealed class MergeGuestAccountCommandHandler : IRequestHandler<MergeGuestAccountCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MergeGuestAccountCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(MergeGuestAccountCommand request, CancellationToken cancellationToken)
    {
        var guestUser = await _userRepository.GetByIdAsync(request.GuestUserId, cancellationToken);
        if (guestUser is null || !guestUser.IsGuest)
        {
            throw new GuestUserNotFoundException();
        }

        var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (targetUser is null)
        {
            throw new TargetUserNotFoundException();
        }

        guestUser.DeleteAccount();

        // Raise event to trigger choreography data transfer across microservices
        targetUser.MergeGuest(guestUser.Id);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
