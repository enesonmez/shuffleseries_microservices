using FluentValidation.TestHelper;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.ConvertGuest;

namespace ShuffleSeries.Identity.Tests.Application.Features.Auth.Commands.ConvertGuest;

public class ConvertGuestCommandValidatorTests
{
    private readonly ConvertGuestCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var command = new ConvertGuestCommand(Guid.NewGuid(), "converted@example.com", "StrongPass1!");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_UserId_IsEmpty()
    {
        var command = new ConvertGuestCommand(Guid.Empty, "converted@example.com", "StrongPass1!");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Should_Fail_When_Email_IsInvalid()
    {
        var command = new ConvertGuestCommand(Guid.NewGuid(), "invalid-email", "StrongPass1!");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
