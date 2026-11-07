using ShuffleSeries.Shared.Core.Domain.Attributes;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid UserId,
    [property: MaskSensitiveData] string CurrentPassword,
    [property: MaskSensitiveData] string NewPassword
);
