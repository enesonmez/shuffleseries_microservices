using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Translations.RemoveEpisodeTranslation;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes.Commands.Translations.RemoveEpisodeTranslation;

public class RemoveEpisodeTranslationCommandValidatorTests
{
    [Fact]
    public void Validate_WhenInvalid_ShouldHaveValidationErrors()
    {
        var validator = new RemoveEpisodeTranslationCommandValidator();
        var invalidCommand = new RemoveEpisodeTranslationCommand(Guid.Empty, "invalid-lang");

        var result = validator.Validate(invalidCommand);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RemoveEpisodeTranslationCommand.EpisodeId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RemoveEpisodeTranslationCommand.LanguageCode));
    }

    [Fact]
    public void Validate_WhenValid_ShouldPassValidation()
    {
        var validator = new RemoveEpisodeTranslationCommandValidator();
        var validCommand = new RemoveEpisodeTranslationCommand(Guid.NewGuid(), "tr");

        var result = validator.Validate(validCommand);

        result.IsValid.Should().BeTrue();
    }
}
