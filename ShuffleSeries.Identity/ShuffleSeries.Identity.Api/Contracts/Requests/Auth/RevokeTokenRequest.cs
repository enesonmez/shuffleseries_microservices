namespace ShuffleSeries.Identity.Api.Contracts.Requests.Auth;

/// <summary>
/// Request payload for revoking an active refresh token on logout.
/// </summary>
public sealed record RevokeTokenRequest(string RefreshToken, string? AccessToken = null);
