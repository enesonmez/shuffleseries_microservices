using MediatR;
using ShuffleSeries.Shared.Core.Domain.Attributes;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.RevokeToken;

public record RevokeTokenCommand(
    [property: MaskSensitiveData] string RefreshToken
) : IRequest<bool>;
