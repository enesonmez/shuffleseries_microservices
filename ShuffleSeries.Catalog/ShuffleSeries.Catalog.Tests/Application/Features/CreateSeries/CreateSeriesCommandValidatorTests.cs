using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;

namespace ShuffleSeries.Catalog.Tests.Application.Features.CreateSeries;

public class CreateSeriesCommandValidatorTests
{
    private readonly CreateSeriesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var command = new CreateSeriesCommand("Breaking Bad", "A high school chemistry teacher turned methamphetamine producer.", false);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullTitle_ShouldHaveValidationError(string? title)
    {
        var command = new CreateSeriesCommand(title!, "Valid Description", false);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Title is required.");
    }

    [Fact]
    public void Validate_WithTitleExceeding150Characters_ShouldHaveValidationError()
    {
        var longTitle = new string('A', 151);
        var command = new CreateSeriesCommand(longTitle, "Valid Description", false);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Title cannot exceed 150 characters.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullDescription_ShouldHaveValidationError(string? description)
    {
        var command = new CreateSeriesCommand("Valid Title", description!, false);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_WithDescriptionExceeding1000Characters_ShouldHaveValidationError()
    {
        var longDescription = new string('D', 1001);
        var command = new CreateSeriesCommand("Valid Title", longDescription, false);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description cannot exceed 1000 characters.");
    }
}
