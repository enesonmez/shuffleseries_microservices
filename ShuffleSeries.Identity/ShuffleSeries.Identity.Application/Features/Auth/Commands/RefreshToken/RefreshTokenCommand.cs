using MediatR;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Shared.Core.Domain.Attributes;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    [property: MaskSensitiveData] string RefreshToken,
    string? IpAddress = null
) : IRequest<TokenResponse>;
