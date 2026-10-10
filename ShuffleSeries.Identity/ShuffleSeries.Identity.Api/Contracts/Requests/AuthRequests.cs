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
/// Request payload for merging an anonymous guest account into a permanent user account.
/// Target user ID is securely resolved from authenticated claims when present.
/// </summary>
public sealed record MergeGuestRequest(
    Guid GuestUserId,
    Guid? TargetUserId = null
);
