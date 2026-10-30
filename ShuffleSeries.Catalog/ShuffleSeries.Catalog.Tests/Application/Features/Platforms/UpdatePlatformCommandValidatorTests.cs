using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Platforms.Commands.UpdatePlatform;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Platforms;

public class UpdatePlatformCommandValidatorTests
{
    private readonly UpdatePlatformCommandValidator _validator = new();

    private static UpdatePlatformCommand CreateValidCommand(
        Guid? id = null,
        string? logoUrl = null,
        string? inactiveLogoUrl = null,
        string? baseWebUrl = null,
        string? baseAppSchemeUrl = null,
        string? slug = null,
        string? name = null)
    {
        return new UpdatePlatformCommand(
            id ?? Guid.NewGuid(),
            name ?? "Netflix",
            slug ?? "netflix",
            logoUrl ?? "https://example.com/logo.png",
            inactiveLogoUrl ?? "https://example.com/inactive.png",
            baseWebUrl ?? "https://netflix.com",
            baseAppSchemeUrl ?? "netflix://",
            0);
    }

    [Fact]
    public void Should_NotHaveError_When_CommandIsValid()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_IdIsEmpty()
    {
        var command = CreateValidCommand(id: Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
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
    [InlineData("data:text/html,hack")]
    [InlineData("file:///etc/passwd")]
    public void Should_HaveError_When_BaseAppSchemeUrl_IsDangerousScheme(string dangerousScheme)
    {
        var command = CreateValidCommand(baseAppSchemeUrl: dangerousScheme);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BaseAppSchemeUrl);
    }

    [Theory]
    [InlineData("INVALID SLUG")]
    [InlineData("slug_with_underscores")]
    [InlineData("slug.dots")]
    public void Should_HaveError_When_Slug_IsInvalidFormat(string invalidSlug)
    {
        var command = CreateValidCommand(slug: invalidSlug);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }
}
