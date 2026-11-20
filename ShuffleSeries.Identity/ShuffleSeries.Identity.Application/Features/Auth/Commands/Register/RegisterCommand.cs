using ShuffleSeries.Shared.Core.Domain.Attributes;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    [property: MaskSensitiveData] string Password,
    string? IpAddress = null
);
