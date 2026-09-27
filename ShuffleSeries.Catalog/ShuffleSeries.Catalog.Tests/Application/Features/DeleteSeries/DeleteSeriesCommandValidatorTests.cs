using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.DeleteSeries;

namespace ShuffleSeries.Catalog.Tests.Application.Features.DeleteSeries;

public class DeleteSeriesCommandValidatorTests
{
    private readonly DeleteSeriesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidId_ShouldNotHaveAnyValidationErrors()
    {
        var command = new DeleteSeriesCommand(Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyId_ShouldHaveValidationError()
    {
        var command = new DeleteSeriesCommand(Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Series Id is required.");
    }
}
