using MediatR;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Shared.Core.Domain.Attributes;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.ConvertGuest;

public sealed record ConvertGuestCommand(
    Guid UserId,
    string Email,
    [property: MaskSensitiveData] string Password,
    string? IpAddress = null,
    string? CurrentJwtId = null
) : IRequest<AuthResponse>;
