namespace ShuffleSeries.Identity.Application.Models;

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType = "Bearer"
);

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

public sealed record ExternalUserPrincipal(
    string Provider,
    string SubjectId,
    string? Email,
    string? Name
);

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    bool IsGuest,
    string Status,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
);
