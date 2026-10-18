using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Events;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Tests.Domain.Entities;

public class MoodTests
{
    [Fact]
    public void Create_Should_CreateMoodAndRaiseEvent()
    {
        // Act
        var mood = Mood.Create("beyin-yakanlar", "#8B5CF6", "brain", 2);

        // Assert
        mood.Should().NotBeNull();
        mood.Id.Should().NotBeEmpty();
        mood.Code.Should().Be("beyin-yakanlar");
        mood.AmbientColorHex.Should().Be("#8B5CF6");
        mood.IconUrl.Should().Be("brain");
        mood.DisplayOrder.Should().Be(2);
        mood.IsActive.Should().BeTrue();

        mood.GetDomainEvents().Should().ContainSingle(e => e is MoodCreatedDomainEvent);
    }

    [Fact]
    public void AddOrUpdateTranslation_Should_AddAndNormalizeLanguageCode()
    {
        // Arrange
        var mood = Mood.Create("beyin-yakanlar", "#8B5CF6", "brain", 2);

        // Act
        var tr = mood.AddOrUpdateTranslation("TR", "Beyin Yakanlar", "Karmaşık kurgulu yapımlar", "Gizem, Bilimkurgu");

        // Assert
        tr.LanguageCode.Should().Be(LanguageCode.Turkish);
        tr.Name.Should().Be("Beyin Yakanlar");
        tr.Description.Should().Be("Karmaşık kurgulu yapımlar");
        tr.VibeTags.Should().Be("Gizem, Bilimkurgu");
        mood.Translations.Should().Contain(tr);
    }

    [Fact]
    public void Update_Should_UpdateDetailsAndRaiseEvent()
    {
        // Arrange
        var mood = Mood.Create("code", "#FFFFFF", "icon", 1);
        mood.ClearDomainEvents();

        // Act
        mood.Update("new-code", "#000000", "new-icon", 5);

        // Assert
        mood.Code.Should().Be("new-code");
        mood.AmbientColorHex.Should().Be("#000000");
        mood.IconUrl.Should().Be("new-icon");
        mood.DisplayOrder.Should().Be(5);
        mood.ModifiedAtUtc.Should().NotBeNull();

        mood.GetDomainEvents().Should().ContainSingle(e => e is MoodUpdatedDomainEvent);
    }

    [Theory]
    [InlineData("", "#FFF", "icon")]
    [InlineData("code", "", "icon")]
    public void Create_ShouldThrowArgumentException_WhenRequiredFieldIsEmpty(string code, string color, string icon)
    {
        var action = () => Mood.Create(code, color, icon, 1);
        action.Should().Throw<ArgumentException>();
    }
}
