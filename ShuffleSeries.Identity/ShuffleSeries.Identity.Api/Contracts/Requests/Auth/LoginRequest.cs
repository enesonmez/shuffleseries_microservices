namespace ShuffleSeries.Identity.Api.Contracts.Requests.Auth;

/// <summary>
/// Request payload for user authentication with email and password.
/// </summary>
public sealed record LoginRequest(string Email, string Password);
