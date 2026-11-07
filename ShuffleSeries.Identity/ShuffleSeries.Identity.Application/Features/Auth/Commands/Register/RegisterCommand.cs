using MediatR;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Shared.Core.Domain.Attributes;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    [property: MaskSensitiveData] string Password,
    string? IpAddress = null
) : IRequest<AuthResponse>;
