using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.DeleteEpisode;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes;

public class DeleteEpisodeCommandValidatorTests
{
    private readonly DeleteEpisodeCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_IdIsValid()
    {
        var command = new DeleteEpisodeCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        var command = new DeleteEpisodeCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}
