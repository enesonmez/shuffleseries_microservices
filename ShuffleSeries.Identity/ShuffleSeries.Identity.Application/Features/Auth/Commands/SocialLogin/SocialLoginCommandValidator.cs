using FluentValidation;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.SocialLogin;

public sealed class SocialLoginCommandValidator : AbstractValidator<SocialLoginCommand>
{
    public SocialLoginCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required.")
            .Must(p => p.Equals("Google", StringComparison.OrdinalIgnoreCase) || p.Equals("Apple", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only 'Google' and 'Apple' providers are supported.");

        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("ID token is required.")
            .MaximumLength(8192).WithMessage("ID token cannot exceed 8192 characters.");

        RuleFor(x => x.IpAddress)
            .MaximumLength(45)
            .When(x => !string.IsNullOrWhiteSpace(x.IpAddress));
    }
}
