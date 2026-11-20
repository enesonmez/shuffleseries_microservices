namespace ShuffleSeries.Identity.Api.Contracts.Requests.Auth;

/// <summary>
/// Request payload for converting an anonymous guest session into a permanent user account.
/// </summary>
public sealed record ConvertGuestRequest(string Email, string Password);
