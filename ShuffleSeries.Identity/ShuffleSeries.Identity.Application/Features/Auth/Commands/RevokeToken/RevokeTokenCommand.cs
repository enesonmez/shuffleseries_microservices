using MediatR;
using ShuffleSeries.Shared.Core.Domain.Attributes;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.RevokeToken;

public sealed record RevokeTokenCommand(
    [property: MaskSensitiveData] string RefreshToken,
    [property: MaskSensitiveData] string? AccessToken = null,
    string? JwtId = null,
    Guid? CallerUserId = null
) : IRequest<bool>;
