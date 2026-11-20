using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Moods.Commands.UpdateMood;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Moods.Commands.UpdateMood;

public class UpdateMoodCommandValidatorTests
{
    private readonly UpdateMoodCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        var command = new UpdateMoodCommand(Guid.NewGuid(), "feel-good", "#FF5733", "https://cdn.example.com/icon.png", 1);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        var command = new UpdateMoodCommand(Guid.Empty, "feel-good", "#FF5733");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [MemberData(nameof(InvalidCodes))]
    public void Should_Fail_When_CodeIsInvalid(string invalidCode)
    {
        var command = new UpdateMoodCommand(Guid.NewGuid(), invalidCode, "#FF5733");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    public static TheoryData<string> InvalidCodes => ["", "INVALID", "code with spaces"];

    [Fact]
    public void Should_Fail_When_AmbientColorHexIsInvalid()
    {
        var command = new UpdateMoodCommand(Guid.NewGuid(), "feel-good", "not-hex");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AmbientColorHex);
    }

    [Fact]
    public void Should_Fail_When_IconUrlAndDisplayOrderAreInvalid()
    {
        var command = new UpdateMoodCommand(Guid.NewGuid(), "feel-good", "#ABC", "ftp://invalid.url", -1);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.IconUrl);
        result.ShouldHaveValidationErrorFor(x => x.DisplayOrder);
    }
}
