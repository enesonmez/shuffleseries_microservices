using FluentValidation.TestHelper;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.RevokeToken;

namespace ShuffleSeries.Identity.Tests.Application.Features.Auth.Commands.RevokeToken;

public class RevokeTokenCommandValidatorTests
{
    private readonly RevokeTokenCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var command = new RevokeTokenCommand("valid-token-to-revoke");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_Fail_When_Token_IsEmpty(string? token)
    {
        var command = new RevokeTokenCommand(token!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public void Should_Fail_When_RefreshToken_Exceeds512Characters()
    {
        var command = new RevokeTokenCommand(new string('x', 513));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
            .WithErrorMessage("Refresh token cannot exceed 512 characters.");
    }

    [Fact]
    public void Should_Fail_When_AccessToken_Exceeds8192Characters()
    {
        var command = new RevokeTokenCommand("valid-token", AccessToken: new string('y', 8193));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AccessToken)
            .WithErrorMessage("Access token cannot exceed 8192 characters.");
    }
}
