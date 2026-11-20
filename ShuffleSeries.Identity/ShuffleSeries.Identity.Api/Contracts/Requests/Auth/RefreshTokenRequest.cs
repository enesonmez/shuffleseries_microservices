namespace ShuffleSeries.Identity.Api.Contracts.Requests.Auth;

/// <summary>
/// Request payload for rotating a refresh token.
/// </summary>
public sealed record RefreshTokenRequest(string RefreshToken);
