using System.Collections.Frozen;
using FluentValidation;

namespace ShuffleSeries.Shared.Core.Application.Validators;

public static class RuleBuilderExtensions
{
    private static readonly FrozenSet<string> _disallowedCustomSchemes = new[]
    {
        "javascript",
        "data",
        "file",
        "vbscript",
        "about",
        "blob"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Validates that the string property is a valid absolute HTTP or HTTPS URL.
    /// Null or empty/whitespace values are treated as valid (use .NotEmpty() for required fields).
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeHttpUrl<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(url => string.IsNullOrWhiteSpace(url) || IsValidHttpUrl(url))
            .WithMessage("'{PropertyName}' must be a valid HTTP or HTTPS URL.");
    }

    /// <summary>
    /// Validates that the string property is a valid custom deep-link scheme (e.g. 'netflix://', 'primevideo://')
    /// and excludes dangerous protocols like 'javascript:', 'data:', 'file:', or 'blob:'.
    /// Null or empty/whitespace values are treated as valid (use .NotEmpty() for required fields).
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeCustomAppScheme<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(scheme => string.IsNullOrWhiteSpace(scheme) || IsValidCustomAppScheme(scheme))
            .WithMessage("'{PropertyName}' must be a valid custom application scheme (e.g. 'netflix://').");
    }

    public static bool IsValidHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public static bool IsValidCustomAppScheme(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(uri.Scheme) && !_disallowedCustomSchemes.Contains(uri.Scheme);
    }
}
