using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;

namespace ShuffleSeries.Catalog.Tests.Application.Features.UpdateSeries;

public class UpdateSeriesCommandValidatorTests
{
    private readonly UpdateSeriesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var command = new UpdateSeriesCommand(Guid.NewGuid(), "Better Call Saul", "Legal drama spin-off.", false);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyId_ShouldHaveValidationError()
    {
        var command = new UpdateSeriesCommand(Guid.Empty, "Valid Title", "Valid Description", false);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Id is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullTitle_ShouldHaveValidationError(string? title)
    {
        var command = new UpdateSeriesCommand(Guid.NewGuid(), title!, "Valid Description", false);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Title is required.");
    }

    [Fact]
    public void Validate_WithTitleExceeding150Characters_ShouldHaveValidationError()
    {
        var longTitle = new string('T', 151);
        var command = new UpdateSeriesCommand(Guid.NewGuid(), longTitle, "Valid Description", false);

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
        var command = new UpdateSeriesCommand(Guid.NewGuid(), "Valid Title", description!, false);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_WithDescriptionExceeding1000Characters_ShouldHaveValidationError()
    {
        var longDesc = new string('D', 1001);
        var command = new UpdateSeriesCommand(Guid.NewGuid(), "Valid Title", longDesc, false);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description cannot exceed 1000 characters.");
    }
}
