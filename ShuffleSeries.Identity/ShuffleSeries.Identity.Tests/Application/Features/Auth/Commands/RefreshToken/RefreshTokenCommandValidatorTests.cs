using FluentValidation.TestHelper;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.RefreshToken;

namespace ShuffleSeries.Identity.Tests.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var command = new RefreshTokenCommand("valid-refresh-token");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_Fail_When_Token_IsEmpty(string? token)
    {
        var command = new RefreshTokenCommand(token!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public void Should_Fail_When_Token_Exceeds512Characters()
    {
        var command = new RefreshTokenCommand(new string('x', 513));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
            .WithErrorMessage("Refresh token cannot exceed 512 characters.");
    }
}
