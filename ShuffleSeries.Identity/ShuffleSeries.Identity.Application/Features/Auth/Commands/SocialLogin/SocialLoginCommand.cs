using MediatR;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Shared.Core.Domain.Attributes;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.SocialLogin;

public sealed record SocialLoginCommand(
    string Provider,
    [property: MaskSensitiveData] string IdToken,
    string? IpAddress = null
) : IRequest<AuthResponse>;
