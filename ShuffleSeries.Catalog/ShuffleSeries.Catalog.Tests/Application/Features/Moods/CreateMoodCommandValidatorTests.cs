using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Moods.Commands.CreateMood;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Moods;

public class CreateMoodCommandValidatorTests
{
    private readonly CreateMoodCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        var command = new CreateMoodCommand(
            Code: "chill-night",
            Name: "Chill Night",
            Description: "Relaxing mood for evenings",
            LanguageCode: "en",
            AmbientColorHex: "#4A90E2",
            IconUrl: "https://example.com/icon.png",
            DisplayOrder: 1,
            IsActive: true);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("INVALID CODE WITH SPACES")]
    public void Should_Fail_When_CodeIsInvalid(string code)
    {
        var command = new CreateMoodCommand(
            Code: code,
            Name: "Chill",
            Description: "Relaxing",
            LanguageCode: "en",
            AmbientColorHex: "#4A90E2");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hex")]
    [InlineData("#ZZZZZZ")]
    public void Should_Fail_When_AmbientColorHexIsInvalid(string hex)
    {
        var command = new CreateMoodCommand(
            Code: "chill",
            Name: "Chill",
            Description: "Relaxing",
            LanguageCode: "en",
            AmbientColorHex: hex);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AmbientColorHex);
    }
}
