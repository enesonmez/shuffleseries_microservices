using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Events;

namespace ShuffleSeries.Catalog.Tests.Domain.Entities;

public class MovieTests
{
    [Fact]
    public void Create_Should_CreateMovieAndRaiseDomainEvent_When_ValidParameters()
    {
        // Act
        var movie = Movie.Create(
            releaseYear: 2010,
            runtimeMinutes: 148,
            posterUrl: "https://example.com/inception.jpg",
            backdropUrl: "https://example.com/inception-bg.jpg",
            imdbId: "tt1375666",
            imdbRating: 8.8,
            imdbVotes: 2500000,
            director: "Christopher Nolan");

        // Assert
        movie.Should().NotBeNull();
        movie.Id.Should().NotBeEmpty();
        movie.MediaType.Should().Be(MediaType.Movie);
        movie.ReleaseYear.Should().Be(2010);
        movie.RuntimeMinutes.Should().Be(148);
        movie.Director.Should().Be("Christopher Nolan");
        movie.PosterUrl.Should().Be("https://example.com/inception.jpg");
        movie.BackdropUrl.Should().Be("https://example.com/inception-bg.jpg");
        movie.ImdbId.Should().Be("tt1375666");
        movie.ImdbRating.Should().Be(8.8);
        movie.ImdbVotes.Should().Be(2500000);

        movie.GetDomainEvents().Should().ContainSingle(e => e is MediaCreatedDomainEvent);
    }

    [Fact]
    public void Create_Should_ThrowArgumentOutOfRangeException_When_RuntimeIsZeroOrNegative()
    {
        // Act
        var action = () => Movie.Create(2020, 0, "https://example.com/poster.jpg");

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Runtime minutes must be greater than zero*");
    }

    [Fact]
    public void UpdateDetails_Should_UpdatePropertiesAndRaiseDomainEvent()
    {
        // Arrange
        var movie = Movie.Create(2010, 148, "https://example.com/poster.jpg");
        movie.ClearDomainEvents();

        // Act
        movie.UpdateDetails(2011, 150, "C. Nolan", "tt1375666", 8.9, 2600000);

        // Assert
        movie.ReleaseYear.Should().Be(2011);
        movie.RuntimeMinutes.Should().Be(150);
        movie.Director.Should().Be("C. Nolan");
        movie.ImdbRating.Should().Be(8.9);
        movie.ImdbVotes.Should().Be(2600000);
        movie.ModifiedAtUtc.Should().NotBeNull();

        movie.GetDomainEvents().Should().ContainSingle(e => e is MediaUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateDetails_Should_ThrowArgumentOutOfRangeException_When_RuntimeIsZeroOrNegative()
    {
        // Arrange
        var movie = Movie.Create(2010, 148, "https://example.com/poster.jpg");

        // Act
        var action = () => movie.UpdateDetails(2010, -5, null, null, null, null);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
