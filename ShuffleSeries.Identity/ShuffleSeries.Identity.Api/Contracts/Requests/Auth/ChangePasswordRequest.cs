namespace ShuffleSeries.Identity.Api.Contracts.Requests.Auth;

/// <summary>
/// Request payload for changing the authenticated user's password.
/// </summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
