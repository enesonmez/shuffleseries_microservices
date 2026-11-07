using FluentValidation;

namespace ShuffleSeries.Identity.Application.Common.Validators;

public static class PasswordValidationExtensions
{
    public static IRuleBuilderOptions<T, string> ApplyPasswordRules<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        string fieldDisplayName = "Password")
    {
        return ruleBuilder
            .NotEmpty().WithMessage($"{fieldDisplayName} is required.")
            .MinimumLength(8).WithMessage($"{fieldDisplayName} must be at least 8 characters long.")
            .MaximumLength(128).WithMessage($"{fieldDisplayName} cannot exceed 128 characters.")
            .Must(p => !string.IsNullOrEmpty(p) && p.Any(char.IsAsciiLetterUpper))
                .WithMessage($"{fieldDisplayName} must contain at least one uppercase letter.")
            .Must(p => !string.IsNullOrEmpty(p) && p.Any(char.IsAsciiLetterLower))
                .WithMessage($"{fieldDisplayName} must contain at least one lowercase letter.")
            .Must(p => !string.IsNullOrEmpty(p) && p.Any(char.IsAsciiDigit))
                .WithMessage($"{fieldDisplayName} must contain at least one number.");
    }
}
