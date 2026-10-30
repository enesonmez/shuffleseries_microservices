using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Movies.Commands.DeleteMovie;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Movies;

public class DeleteMovieCommandValidatorTests
{
    private readonly DeleteMovieCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_IdIsValid()
    {
        var command = new DeleteMovieCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        var command = new DeleteMovieCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}
