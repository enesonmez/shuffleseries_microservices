namespace ShuffleSeries.Identity.Application.Features.Auth.Responses;

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType = "Bearer"
);
