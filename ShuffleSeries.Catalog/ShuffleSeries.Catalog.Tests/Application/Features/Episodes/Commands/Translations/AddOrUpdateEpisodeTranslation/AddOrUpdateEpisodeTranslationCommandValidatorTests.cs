using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Translations.AddOrUpdateEpisodeTranslation;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes.Commands.Translations.AddOrUpdateEpisodeTranslation;

public class AddOrUpdateEpisodeTranslationCommandValidatorTests
{
    [Fact]
    public void Validate_WhenInvalid_ShouldHaveValidationErrors()
    {
        var validator = new AddOrUpdateEpisodeTranslationCommandValidator();
        var invalidCommand = new AddOrUpdateEpisodeTranslationCommand(Guid.Empty, "invalid-lang", "");

        var result = validator.Validate(invalidCommand);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddOrUpdateEpisodeTranslationCommand.EpisodeId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddOrUpdateEpisodeTranslationCommand.Title));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddOrUpdateEpisodeTranslationCommand.LanguageCode));
    }
}
