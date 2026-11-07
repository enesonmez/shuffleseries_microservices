using FluentValidation;
using ShuffleSeries.Identity.Application.Common.Validators;

namespace ShuffleSeries.Identity.Tests.Application.Common.Validators;

public class PasswordValidationExtensionsTests
{
    private sealed class DummyPasswordModel
    {
        public string Password { get; set; } = string.Empty;
    }

    private sealed class DummyPasswordModelValidator : AbstractValidator<DummyPasswordModel>
    {
        public DummyPasswordModelValidator()
        {
            RuleFor(x => x.Password).ApplyPasswordRules();
        }
    }

    private readonly DummyPasswordModelValidator _validator = new();

    [Theory]
    [InlineData("Valid123!")]
    [InlineData("SecurePassword2026")]
    [InlineData("Aa1bbbbbb")]
    public void Validate_WhenPasswordMeetsAllCriteria_ShouldPass(string password)
    {
        var model = new DummyPasswordModel { Password = password };
        var result = _validator.Validate(model);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenPasswordIsEmpty_ShouldFailWithRequiredMessage(string password)
    {
        var model = new DummyPasswordModel { Password = password };
        var result = _validator.Validate(model);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Password is required.");
    }

    [Fact]
    public void Validate_WhenPasswordShorterThan8Chars_ShouldFail()
    {
        var model = new DummyPasswordModel { Password = "Ab1" };
        var result = _validator.Validate(model);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Password must be at least 8 characters long.");
    }

    [Fact]
    public void Validate_WhenPasswordExceeds128Chars_ShouldFail()
    {
        var model = new DummyPasswordModel { Password = "A1" + new string('a', 127) };
        var result = _validator.Validate(model);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Password cannot exceed 128 characters.");
    }

    [Fact]
    public void Validate_WhenPasswordMissingUppercase_ShouldFail()
    {
        var model = new DummyPasswordModel { Password = "password123" };
        var result = _validator.Validate(model);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Password must contain at least one uppercase letter.");
    }

    [Fact]
    public void Validate_WhenPasswordMissingLowercase_ShouldFail()
    {
        var model = new DummyPasswordModel { Password = "PASSWORD123" };
        var result = _validator.Validate(model);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Password must contain at least one lowercase letter.");
    }

    [Fact]
    public void Validate_WhenPasswordMissingNumber_ShouldFail()
    {
        var model = new DummyPasswordModel { Password = "PasswordOnly" };
        var result = _validator.Validate(model);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Password must contain at least one number.");
    }
}
