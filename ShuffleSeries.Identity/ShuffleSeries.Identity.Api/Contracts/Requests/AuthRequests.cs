namespace ShuffleSeries.Identity.Api.Contracts.Requests;

/// <summary>
/// Request payload for standard user registration with email and password.
/// Client IP address is resolved server-side from HttpContext and is not exposed in the API contract.
/// </summary>
public sealed record RegisterRequest(string Email, string Password);

/// <summary>
/// Request payload for user authentication with email and password.
/// </summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>
/// Request payload for rotating a refresh token.
/// </summary>
public sealed record RefreshTokenRequest(string RefreshToken);

/// <summary>
/// Request payload for revoking an active refresh token on logout.
/// </summary>
public sealed record RevokeTokenRequest(string RefreshToken, string? AccessToken = null);

/// <summary>
/// Request payload for Apple or Google Single Sign-On (SSO).
/// </summary>
public sealed record SocialLoginRequest(
    string Provider,
    string IdToken,
    string? AppleRefreshToken = null
);

/// <summary>
/// Request payload for converting an anonymous guest session into a permanent user account.
/// </summary>
public sealed record ConvertGuestRequest(string Email, string Password);

/// <summary>
/// Request payload for changing the authenticated user's password.
/// </summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

