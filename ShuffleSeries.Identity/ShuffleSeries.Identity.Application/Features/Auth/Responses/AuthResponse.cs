namespace ShuffleSeries.Identity.Application.Features.Auth.Responses;

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    bool IsGuest,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string AccessToken,
    string RefreshToken,
    int ExpiresIn
);
