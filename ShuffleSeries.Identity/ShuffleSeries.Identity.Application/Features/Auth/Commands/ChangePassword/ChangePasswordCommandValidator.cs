using FluentValidation;
using ShuffleSeries.Identity.Application.Common.Validators;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.")
            .MaximumLength(128).WithMessage("Current password cannot exceed 128 characters.");

        RuleFor(x => x.NewPassword)
            .ApplyPasswordRules("New password")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password.");
    }
}
