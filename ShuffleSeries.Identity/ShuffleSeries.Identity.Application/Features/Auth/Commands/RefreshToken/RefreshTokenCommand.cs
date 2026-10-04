using MediatR;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Shared.Core.Domain.Attributes;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(
    [property: MaskSensitiveData] string RefreshToken,
    string? IpAddress = null
) : IRequest<TokenResponse>;
