using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Genres.Commands.CreateGenre;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Genres;

public class CreateGenreCommandValidatorTests
{
    private readonly CreateGenreCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        var command = new CreateGenreCommand(
            Code: "action-adventure",
            Name: "Action & Adventure",
            LanguageCode: "en",
            Description: "Exciting movies",
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
        var command = new CreateGenreCommand(
            Code: code,
            Name: "Action",
            LanguageCode: "en",
            Description: null,
            DisplayOrder: 1,
            IsActive: true);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Should_Fail_When_NameIsEmpty()
    {
        var command = new CreateGenreCommand(
            Code: "action",
            Name: "",
            LanguageCode: "en",
            Description: null,
            DisplayOrder: 1,
            IsActive: true);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
