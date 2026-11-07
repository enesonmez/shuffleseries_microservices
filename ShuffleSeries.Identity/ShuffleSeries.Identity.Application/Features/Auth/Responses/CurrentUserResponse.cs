namespace ShuffleSeries.Identity.Application.Features.Auth.Responses;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    bool IsGuest,
    string Status,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
);
