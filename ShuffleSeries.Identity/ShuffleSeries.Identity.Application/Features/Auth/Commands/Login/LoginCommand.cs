using MediatR;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Shared.Core.Domain.Attributes;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    [property: MaskSensitiveData] string Password,
    string? IpAddress = null
) : IRequest<AuthResponse>;
