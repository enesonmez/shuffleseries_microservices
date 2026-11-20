namespace ShuffleSeries.Identity.Api.Contracts.Requests.Auth;

/// <summary>
/// Request payload for standard user registration with email and password.
/// Client IP address is resolved server-side from HttpContext and is not exposed in the API contract.
/// </summary>
public sealed record RegisterRequest(string Email, string Password);
