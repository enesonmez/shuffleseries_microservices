using ShuffleSeries.Identity.Application.Common.Models;

namespace ShuffleSeries.Identity.Application.Common.Interfaces;

/// <summary>
/// Defines a contract for external social authentication providers (Google, Apple, etc.).
/// Implements the Strategy / Provider pattern to enable Open/Closed Principle (OCP) extension.
/// </summary>
public interface ISocialAuthProvider
{
    /// <summary>
    /// Gets the unique name of the external provider (e.g. "Google", "Apple").
    /// </summary>
    string Provider { get; }

    /// <summary>
    /// Validates the provided ID token and resolves user identity information.
    /// </summary>
    Task<ExternalUserPrincipal?> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
