using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Genres.Commands.UpdateGenre;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Genres.Commands.UpdateGenre;

public class UpdateGenreCommandValidatorTests
{
    private readonly UpdateGenreCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        var command = new UpdateGenreCommand(Guid.NewGuid(), "sci-fi", 2);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        var command = new UpdateGenreCommand(Guid.Empty, "sci-fi");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("SCI-FI")]
    [InlineData("sci fi")]
    public void Should_Fail_When_CodeIsInvalid(string invalidCode)
    {
        var command = new UpdateGenreCommand(Guid.NewGuid(), invalidCode);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Should_Fail_When_DisplayOrderIsNegative()
    {
        var command = new UpdateGenreCommand(Guid.NewGuid(), "sci-fi", -1);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DisplayOrder);
    }
}
