using FluentValidation.TestHelper;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.Login;

namespace ShuffleSeries.Identity.Tests.Application.Features.Auth.Commands.Login;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var command = new LoginCommand("test@example.com", "Password123!");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_Fail_When_Email_IsEmpty(string? email)
    {
        var command = new LoginCommand(email!, "Password123!");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    public void Should_Fail_When_Email_IsInvalid(string email)
    {
        var command = new LoginCommand(email, "Password123!");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Should_Fail_When_Password_IsEmpty(string? password)
    {
        var command = new LoginCommand("test@example.com", password!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Fail_When_Email_Exceeds256Characters()
    {
        var longEmail = new string('a', 245) + "@example.com";
        var command = new LoginCommand(longEmail, "Password123!");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email cannot exceed 256 characters.");
    }
}
