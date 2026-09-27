namespace ShuffleSeries.Shared.Core.Domain.Attributes;

/// <summary>
/// Marks a property or field as containing sensitive PII (Personally Identifiable Information),
/// credentials, tokens, or financial data that must be masked in structured logs and telemetry.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class MaskSensitiveDataAttribute : Attribute
{
    public MaskSensitiveDataAttribute(string mask = "***MASKED***")
    {
        Mask = mask;
    }

    /// <summary>
    /// The masking string to display instead of the raw sensitive value.
    /// </summary>
    public string Mask { get; }
}
