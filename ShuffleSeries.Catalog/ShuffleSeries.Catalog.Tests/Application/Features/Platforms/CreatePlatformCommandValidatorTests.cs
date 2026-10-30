using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Platforms.Commands.CreatePlatform;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Platforms;

public class CreatePlatformCommandValidatorTests
{
    private readonly CreatePlatformCommandValidator _validator = new();

    private static CreatePlatformCommand CreateValidCommand(
        string? logoUrl = null,
        string? inactiveLogoUrl = null,
        string? baseWebUrl = null,
        string? baseAppSchemeUrl = null,
        string? slug = null,
        string? name = null)
    {
        return new CreatePlatformCommand(
            name ?? "Netflix",
            slug ?? "netflix",
            logoUrl ?? "https://example.com/logo.png",
            inactiveLogoUrl ?? "https://example.com/inactive.png",
            baseWebUrl ?? "https://netflix.com",
            baseAppSchemeUrl ?? "netflix://",
            0,
            true);
    }

    [Fact]
    public void Should_NotHaveError_When_CommandIsValid()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_NotHaveError_When_BaseAppSchemeUrl_IsNull()
    {
        var command = CreateValidCommand(baseAppSchemeUrl: null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.BaseAppSchemeUrl);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    public void Should_HaveError_When_LogoUrl_IsInvalid(string invalidUrl)
    {
        var command = CreateValidCommand(logoUrl: invalidUrl);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.LogoUrl);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    public void Should_HaveError_When_InactiveLogoUrl_IsInvalid(string invalidUrl)
    {
        var command = CreateValidCommand(inactiveLogoUrl: invalidUrl);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.InactiveLogoUrl);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    public void Should_HaveError_When_BaseWebUrl_IsInvalid(string invalidUrl)
    {
        var command = CreateValidCommand(baseWebUrl: invalidUrl);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BaseWebUrl);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,test")]
    [InlineData("file:///etc/passwd")]
    [InlineData("not a valid uri")]
    public void Should_HaveError_When_BaseAppSchemeUrl_IsDangerousOrMalformed(string invalidScheme)
    {
        var command = CreateValidCommand(baseAppSchemeUrl: invalidScheme);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BaseAppSchemeUrl);
    }

    [Theory]
    [InlineData("NETFLIX")]
    [InlineData("net flix")]
    [InlineData("netflix!")]
    public void Should_HaveError_When_Slug_IsNotLowercaseAlphaNumericWithHyphen(string invalidSlug)
    {
        var command = CreateValidCommand(slug: invalidSlug);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Should_HaveError_When_Name_IsEmpty()
    {
        var command = CreateValidCommand(name: "");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
