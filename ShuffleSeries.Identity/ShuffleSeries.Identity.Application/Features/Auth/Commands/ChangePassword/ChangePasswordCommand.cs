using FluentValidation;
using MediatR;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Attributes;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(
    Guid UserId,
    [property: MaskSensitiveData] string CurrentPassword,
    [property: MaskSensitiveData] string NewPassword
) : IRequest<bool>;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.")
            .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("New password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("New password must contain at least one number.")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password.");
    }
}

internal sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenBlacklistService? _blacklistService;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenBlacklistService? blacklistService = null)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _blacklistService = blacklistService;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException();
        }

        if (user.PasswordHash is null || !_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidCurrentPasswordException();
        }

        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(newPasswordHash);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (_blacklistService is not null)
        {
            await _blacklistService.BlacklistUserTokensAsync(user.Id, TimeSpan.FromHours(1), cancellationToken);
        }

        return true;
    }
}
