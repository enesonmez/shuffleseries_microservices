using FluentValidation;
using FluentValidation.TestHelper;
using ShuffleSeries.Shared.Core.Application.Validators;

namespace ShuffleSeries.Shared.Core.Tests.Application.Validators;

public class RuleBuilderExtensionsTests
{
    private sealed record TestModel(string? Url, string? AppScheme);

    private sealed class TestModelValidator : AbstractValidator<TestModel>
    {
        public TestModelValidator()
        {
            RuleFor(x => x.Url).MustBeHttpUrl();
            RuleFor(x => x.AppScheme).MustBeCustomAppScheme();
        }
    }

    private readonly TestModelValidator _validator = new();

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    [InlineData("https://example.com/images/logo.png")]
    [InlineData("https://sub.domain.co.uk:8080/path?param=value#hash")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MustBeHttpUrl_Should_Pass_For_ValidHttpUrls_Or_Empty(string? url)
    {
        // Arrange
        var model = new TestModel(url, null);

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Url);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/file.txt")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    [InlineData("file:///etc/passwd")]
    [InlineData("mailto:user@example.com")]
    public void MustBeHttpUrl_Should_Fail_For_InvalidOrNonHttpUrls(string? url)
    {
        // Arrange
        var model = new TestModel(url, null);

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url);
    }

    [Theory]
    [InlineData("netflix://")]
    [InlineData("netflix://title/80057281")]
    [InlineData("primevideo://")]
    [InlineData("disneyplus://media/123")]
    [InlineData("customapp://open?id=42")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MustBeCustomAppScheme_Should_Pass_For_ValidCustomSchemes_Or_Empty(string? scheme)
    {
        // Arrange
        var model = new TestModel(null, scheme);

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AppScheme);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("file:///etc/passwd")]
    [InlineData("blob:http://example.com/uuid")]
    [InlineData("vbscript:msgbox")]
    [InlineData("about:blank")]
    [InlineData("not a valid uri")]
    public void MustBeCustomAppScheme_Should_Fail_For_DangerousSchemes_Or_MalformedUris(string? scheme)
    {
        // Arrange
        var model = new TestModel(null, scheme);

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AppScheme);
    }

    [Fact]
    public void IsValidHttpUrl_Should_ReturnFalse_For_NullOrWhitespace()
    {
        RuleBuilderExtensions.IsValidHttpUrl(null).Should().BeFalse();
        RuleBuilderExtensions.IsValidHttpUrl("").Should().BeFalse();
        RuleBuilderExtensions.IsValidHttpUrl("   ").Should().BeFalse();
    }

    [Fact]
    public void IsValidCustomAppScheme_Should_ReturnFalse_For_NullOrWhitespace()
    {
        RuleBuilderExtensions.IsValidCustomAppScheme(null).Should().BeFalse();
        RuleBuilderExtensions.IsValidCustomAppScheme("").Should().BeFalse();
        RuleBuilderExtensions.IsValidCustomAppScheme("   ").Should().BeFalse();
    }
}
