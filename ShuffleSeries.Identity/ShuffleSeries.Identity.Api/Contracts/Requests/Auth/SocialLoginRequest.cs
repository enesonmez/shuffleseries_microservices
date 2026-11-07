namespace ShuffleSeries.Identity.Api.Contracts.Requests.Auth;

/// <summary>
/// Request payload for Apple or Google Single Sign-On (SSO).
/// </summary>
public sealed record SocialLoginRequest(
    string Provider,
    string IdToken
);
