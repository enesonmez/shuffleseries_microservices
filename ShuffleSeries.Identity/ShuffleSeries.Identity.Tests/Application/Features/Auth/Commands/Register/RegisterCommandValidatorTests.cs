using FluentValidation.TestHelper;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.Register;

namespace ShuffleSeries.Identity.Tests.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var command = new RegisterCommand("newuser@example.com", "SecurePass1!");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public void Should_Fail_When_Email_IsInvalid(string email)
    {
        var command = new RegisterCommand(email, "SecurePass1!");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("short")] // < 8 chars
    [InlineData("alllowercase1!")] // no uppercase
    [InlineData("ALLUPPERCASE1!")] // no lowercase
    [InlineData("NoNumberAtAll!")] // no digit
    public void Should_Fail_When_Password_DoesNotMeetComplexity(string password)
    {
        var command = new RegisterCommand("newuser@example.com", password);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
