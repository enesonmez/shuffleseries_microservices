using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Platforms.Commands.DeletePlatform;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Platforms;

public class DeletePlatformCommandValidatorTests
{
    private readonly DeletePlatformCommandValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_IdIsNotEmpty()
    {
        var command = new DeletePlatformCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Should_HaveError_When_IdIsEmpty()
    {
        var command = new DeletePlatformCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Platform ID is required.");
    }
}
