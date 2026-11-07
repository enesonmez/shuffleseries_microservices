using FluentValidation;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.");

        RuleFor(x => x.IpAddress)
            .MaximumLength(45)
            .When(x => !string.IsNullOrWhiteSpace(x.IpAddress));
    }
}
