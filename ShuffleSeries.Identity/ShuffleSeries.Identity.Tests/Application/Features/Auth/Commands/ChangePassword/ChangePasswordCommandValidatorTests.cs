using FluentValidation.TestHelper;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.ChangePassword;

namespace ShuffleSeries.Identity.Tests.Application.Features.Auth.Commands.ChangePassword;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "OldPass123!", "NewPass123!");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_UserId_IsEmpty()
    {
        var command = new ChangePasswordCommand(Guid.Empty, "OldPass123!", "NewPass123!");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Should_Fail_When_NewPassword_Equals_CurrentPassword()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "SamePass123!", "SamePass123!");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
