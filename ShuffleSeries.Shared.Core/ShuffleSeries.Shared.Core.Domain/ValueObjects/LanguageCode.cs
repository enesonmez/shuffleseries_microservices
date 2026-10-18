using System.Diagnostics.CodeAnalysis;

namespace ShuffleSeries.Shared.Core.Domain.ValueObjects;

/// <summary>
/// Domain-Driven Design (DDD) Value Object representing an ISO 639-1 language code.
/// Provides compile-time safety, centralized validation, and avoids primitive obsession.
/// </summary>
public sealed record LanguageCode : IComparable<LanguageCode>
{
    public static readonly LanguageCode Turkish = new("tr", "Türkçe", "Turkish");
    public static readonly LanguageCode English = new("en", "English", "English");
    public static readonly LanguageCode German = new("de", "Deutsch", "German");
    public static readonly LanguageCode Spanish = new("es", "Español", "Spanish");
    public static readonly LanguageCode French = new("fr", "Français", "French");

    public static readonly IReadOnlyCollection<LanguageCode> Supported =
        [Turkish, English, German, Spanish, French];

    public static readonly IReadOnlyCollection<string> SupportedValues =
        Supported.Select(s => s.Value).ToArray();

    public string Value { get; }
    public string NativeName { get; }
    public string EnglishName { get; }

    private LanguageCode(string value, string nativeName, string englishName)
    {
        Value = value;
        NativeName = nativeName;
        EnglishName = englishName;
    }

    /// <summary>
    /// Factory method to create a validated LanguageCode instance from string input.
    /// Normalizes input (trims, converts to lowercase) and validates against supported languages.
    /// </summary>
    public static LanguageCode From(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Language code cannot be null, empty, or whitespace.", nameof(code));

        var normalized = code.Trim().ToLowerInvariant();

        return Supported.FirstOrDefault(l => l.Value == normalized)
            ?? throw new ArgumentException(
                $"Language code '{code}' is not supported. Supported languages: {string.Join(", ", SupportedValues)}",
                nameof(code));
    }

    /// <summary>
    /// Attempts to parse a language code safely without throwing exceptions.
    /// </summary>
    public static bool TryFrom(string? code, [NotNullWhen(true)] out LanguageCode? languageCode)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            languageCode = null;
            return false;
        }

        var normalized = code.Trim().ToLowerInvariant();
        languageCode = Supported.FirstOrDefault(l => l.Value == normalized);
        return languageCode is not null;
    }

    /// <summary>
    /// Checks whether the given string represents a supported language code.
    /// </summary>
    public static bool IsSupported(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var normalized = code.Trim().ToLowerInvariant();
        return Supported.Any(l => l.Value == normalized);
    }

    public static implicit operator string(LanguageCode languageCode) => languageCode.Value;

    public static explicit operator LanguageCode(string code) => From(code);

    public override string ToString() => Value;

    public int CompareTo(LanguageCode? other) =>
        string.Compare(Value, other?.Value, StringComparison.Ordinal);
}
