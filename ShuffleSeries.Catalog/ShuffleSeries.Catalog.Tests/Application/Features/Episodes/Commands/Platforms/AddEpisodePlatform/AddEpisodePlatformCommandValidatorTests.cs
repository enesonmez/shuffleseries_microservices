using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Platforms.AddEpisodePlatform;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes.Commands.Platforms.AddEpisodePlatform;

public class AddEpisodePlatformCommandValidatorTests
{
    [Fact]
    public void Validate_WhenInvalid_ShouldHaveValidationErrors()
    {
        var validator = new AddEpisodePlatformCommandValidator();
        var invalidCommand = new AddEpisodePlatformCommand(Guid.Empty, Guid.Empty, "not-a-valid-url", "ftp://invalid-scheme");

        var result = validator.Validate(invalidCommand);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddEpisodePlatformCommand.EpisodeId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddEpisodePlatformCommand.PlatformId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddEpisodePlatformCommand.WebUrl));
    }
}
