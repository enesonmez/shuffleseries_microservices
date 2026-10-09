namespace ShuffleSeries.Identity.Infrastructure.Configuration;

/// <summary>
/// Configuration options for external social login authentication (Google, Apple, etc.).
/// </summary>
public sealed class ExternalAuthOptions
{
    public const string SectionName = "ExternalAuth";

    /// <summary>
    /// When true, verifies ID token cryptographic signatures and audience against provider JWKS endpoints.
    /// When false, allows developer bypass (unvalidated JWT payload reading and mock tokens) for local testing.
    /// </summary>
    public bool ValidateSignatures { get; set; } = false;

    /// <summary>
    /// Google OAuth / OpenID Connect settings.
    /// </summary>
    public GoogleAuthOptions Google { get; set; } = new();

    /// <summary>
    /// Apple Sign-In settings.
    /// </summary>
    public AppleAuthOptions Apple { get; set; } = new();
}

public sealed class GoogleAuthOptions
{
    public string? ClientId { get; set; }
}

public sealed class AppleAuthOptions
{
    public string? ClientId { get; set; }
}
