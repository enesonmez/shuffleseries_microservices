using FluentAssertions;
using ShuffleSeries.Catalog.Domain.Entities;

namespace ShuffleSeries.Catalog.Tests.Domain.Entities;

public class SeriesTests
{
    [Fact]
    public void Create_Should_CreareSeries_When_ValidParameters()
    {
        // Arrange
        var title = "Breaking Bad";
        var description = "A high school chemistry teacher turned meth kingpin.";
        var isIndependent = false;

        // Act
        var series = Series.Create(title, description, isIndependent);

        // Assert
        series.Should().NotBeNull();
        series.Id.Should().NotBeEmpty();
        series.Title.Should().Be(title);
        series.Description.Should().Be(description);
        series.IsIndependentEpisodes.Should().BeFalse();
        series.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("", "Valid Description")]
    [InlineData(" ", "Valid Description")]
    [InlineData(null, "Valid Description")]
    public void Create_Should_ThrowArgumentException_When_TitleIsNullOrWhiteSpace(string? invalidTitle,
        string description)
    {
        // Act
        Action action = () => Series.Create(invalidTitle!, description, false);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Title cannot be empty");
    }
    
    [Theory]
    [InlineData("Valid Title", "")]
    [InlineData("Valid Title", " ")]
    [InlineData("Valid Title", null)]
    public void Create_Should_ThrowArgumentException_When_DescriptionIsNullOrWhiteSpace(string title, string? invalidDescription)
    {
        // Act
        Action action = () => Series.Create(title, invalidDescription!, false);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Description cannot be empty");
    }
    
    [Fact]
    public void Update_Should_UpdateProperties_When_ValidParameters()
    {
        // Arrange
        var series = Series.Create("Old Title", "Old Description", false);
        const string newTitle = "New Title";
        const string newDescription = "New Description";
        const bool newIsIndependent = true;

        // Act
        series.Update(newTitle, newDescription, newIsIndependent);

        // Assert
        series.Title.Should().Be(newTitle);
        series.Description.Should().Be(newDescription);
        series.IsIndependentEpisodes.Should().BeTrue();
        series.ModifiedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Theory]
    [InlineData("", "Valid Description")]
    [InlineData(" ", "Valid Description")]
    [InlineData(null, "Valid Description")]
    public void Update_Should_ThrowArgumentException_When_TitleIsNullOrWhiteSpace(string? invalidTitle, string description)
    {
        // Arrange
        var series = Series.Create("Valid Title", "Valid Description", false);

        // Act
        var action = () => series.Update(invalidTitle!, description, false);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Title cannot be empty");
    }
    
    [Theory]
    [InlineData("Valid Title", "")]
    [InlineData("Valid Title", " ")]
    [InlineData("Valid Title", null)]
    public void Update_Should_ThrowArgumentException_When_DescriptionIsNullOrWhiteSpace(string title, string? invalidDescription)
    {
        // Arrange
        var series = Series.Create("Valid Title", "Valid Description", false);

        // Act
        var action = () => series.Update(title!, invalidDescription!, false);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Description cannot be empty");
    }
}