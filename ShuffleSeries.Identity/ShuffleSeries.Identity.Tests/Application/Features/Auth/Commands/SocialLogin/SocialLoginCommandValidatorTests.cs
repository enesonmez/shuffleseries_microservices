using FluentValidation.TestHelper;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.SocialLogin;

namespace ShuffleSeries.Identity.Tests.Application.Features.Auth.Commands.SocialLogin;

public class SocialLoginCommandValidatorTests
{
    private readonly SocialLoginCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid_Google()
    {
        var command = new SocialLoginCommand("Google", "valid-id-token");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_When_Valid_Apple()
    {
        var command = new SocialLoginCommand("Apple", "valid-id-token");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Facebook")]
    [InlineData("Twitter")]
    [InlineData("Github")]
    public void Should_Fail_When_Provider_IsNotSupported(string provider)
    {
        var command = new SocialLoginCommand(provider, "valid-id-token");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Provider);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Should_Fail_When_IdToken_IsEmpty(string? token)
    {
        var command = new SocialLoginCommand("Google", token!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IdToken);
    }

    [Fact]
    public void Should_Fail_When_IdToken_Exceeds8192Characters()
    {
        var command = new SocialLoginCommand("Google", new string('t', 8193));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IdToken)
            .WithErrorMessage("ID token cannot exceed 8192 characters.");
    }
}
