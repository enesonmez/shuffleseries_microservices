using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Platforms.RemoveEpisodePlatform;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes.Commands.Platforms.RemoveEpisodePlatform;

public class RemoveEpisodePlatformCommandValidatorTests
{
    [Fact]
    public void Validate_WhenInvalid_ShouldHaveValidationErrors()
    {
        var validator = new RemoveEpisodePlatformCommandValidator();
        var invalidCommand = new RemoveEpisodePlatformCommand(Guid.Empty, Guid.Empty);

        var result = validator.Validate(invalidCommand);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RemoveEpisodePlatformCommand.EpisodeId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RemoveEpisodePlatformCommand.PlatformId));
    }

    [Fact]
    public void Validate_WhenValid_ShouldPassValidation()
    {
        var validator = new RemoveEpisodePlatformCommandValidator();
        var validCommand = new RemoveEpisodePlatformCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(validCommand);

        result.IsValid.Should().BeTrue();
    }
}
