using FluentValidation;
using ShuffleSeries.Identity.Application.Common.Validators;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.ConvertGuest;

public sealed class ConvertGuestCommandValidator : AbstractValidator<ConvertGuestCommand>
{
    public ConvertGuestCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Password)
            .ApplyPasswordRules();

        RuleFor(x => x.IpAddress)
            .MaximumLength(45)
            .When(x => !string.IsNullOrWhiteSpace(x.IpAddress));
    }
}
