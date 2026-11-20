using FluentValidation;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.RevokeToken;

public sealed class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.")
            .MaximumLength(512).WithMessage("Refresh token cannot exceed 512 characters.");

        RuleFor(x => x.AccessToken)
            .MaximumLength(8192).WithMessage("Access token cannot exceed 8192 characters.")
            .When(x => !string.IsNullOrEmpty(x.AccessToken));
    }
}
