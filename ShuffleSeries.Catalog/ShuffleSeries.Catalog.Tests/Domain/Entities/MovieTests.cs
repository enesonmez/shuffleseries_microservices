using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Events;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Tests.Domain.Entities;

public class MovieTests
{
    [Fact]
    public void Create_Should_CreateMovieAndRaiseDomainEvent_When_ValidParameters()
    {
        // Act
        var movie = Movie.Create(
            tmdbId: 27205,
            imdbId: "tt1375666",
            releaseYear: 2010,
            runtimeMinutes: 148,
            posterUrl: "https://example.com/inception.jpg",
            backdropUrl: "https://example.com/inception-bg.jpg",
            imdbRating: 8.8,
            imdbVotes: 2500000,
            director: "Christopher Nolan");

        // Assert
        movie.Should().NotBeNull();
        movie.Id.Should().NotBeEmpty();
        movie.MediaType.Should().Be(MediaType.Movie);
        movie.TmdbId.Should().Be(27205);
        movie.ImdbId.Should().Be("tt1375666");
        movie.ReleaseYear.Should().Be(2010);
        movie.RuntimeMinutes.Should().Be(148);
        movie.Director.Should().Be("Christopher Nolan");
        movie.PosterUrl.Should().Be("https://example.com/inception.jpg");
        movie.BackdropUrl.Should().Be("https://example.com/inception-bg.jpg");
        movie.ImdbRating.Should().Be(8.8);
        movie.ImdbVotes.Should().Be(2500000);

        movie.GetDomainEvents().Should().ContainSingle(e => e is MediaCreatedDomainEvent);
    }

    [Fact]
    public void Create_Should_ThrowArgumentOutOfRangeException_When_RuntimeIsZeroOrNegative()
    {
        // Act
        var action = () => Movie.Create(27205, "tt1375666", 2020, 0, "https://example.com/poster.jpg");

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Runtime minutes must be greater than zero*");
    }

    [Fact]
    public void Create_Should_ThrowArgumentException_When_ImdbIdIsEmpty()
    {
        // Act
        var action = () => Movie.Create(27205, "", 2020, 120, "https://example.com/poster.jpg");

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*IMDb ID cannot be empty*");
    }

    [Fact]
    public void UpdateDetails_Should_UpdatePropertiesAndRaiseDomainEvent()
    {
        // Arrange
        var movie = Movie.Create(27205, "tt1375666", 2010, 148, "https://example.com/poster.jpg");
        movie.ClearDomainEvents();

        // Act
        movie.UpdateDetails(27205, "tt1375666", 2011, 150, "C. Nolan", 8.9, 2600000, "https://example.com/new-poster.jpg", "https://example.com/new-backdrop.jpg");

        // Assert
        movie.TmdbId.Should().Be(27205);
        movie.ImdbId.Should().Be("tt1375666");
        movie.ReleaseYear.Should().Be(2011);
        movie.RuntimeMinutes.Should().Be(150);
        movie.Director.Should().Be("C. Nolan");
        movie.ImdbRating.Should().Be(8.9);
        movie.ImdbVotes.Should().Be(2600000);
        movie.PosterUrl.Should().Be("https://example.com/new-poster.jpg");
        movie.BackdropUrl.Should().Be("https://example.com/new-backdrop.jpg");

        movie.GetDomainEvents().Should().ContainSingle(e => e is MediaUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateDetails_Should_ThrowArgumentOutOfRangeException_When_RuntimeIsZeroOrNegative()
    {
        // Arrange
        var movie = Movie.Create(27205, "tt1375666", 2010, 148, "https://example.com/poster.jpg");

        // Act
        var action = () => movie.UpdateDetails(27205, "tt1375666", 2010, -5, null, null, null);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_Should_Throw_When_MediaInvariantsAreViolated()
    {
        var emptyPoster = () => Movie.Create(27205, "tt1375666", 2020, 120, " ");
        emptyPoster.Should().Throw<ArgumentException>();

        var tooOldYear = () => Movie.Create(27205, "tt1375666", 1800, 120, "https://example.com/poster.jpg");
        tooOldYear.Should().Throw<ArgumentOutOfRangeException>();

        var farFutureYear = () => Movie.Create(27205, "tt1375666", 2101, 120, "https://example.com/poster.jpg");
        farFutureYear.Should().Throw<ArgumentOutOfRangeException>();

        var negativeRating = () => Movie.Create(27205, "tt1375666", 2020, 120, "https://example.com/poster.jpg", imdbRating: -0.5);
        negativeRating.Should().Throw<ArgumentOutOfRangeException>();

        var highRating = () => Movie.Create(27205, "tt1375666", 2020, 120, "https://example.com/poster.jpg", imdbRating: 10.5);
        highRating.Should().Throw<ArgumentOutOfRangeException>();

        var negativeVotes = () => Movie.Create(27205, "tt1375666", 2020, 120, "https://example.com/poster.jpg", imdbVotes: -1);
        negativeVotes.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TranslationOperations_Should_ValidateEmptyInputs_UpdateExisting_AndHandleNullRemove()
    {
        var movie = Movie.Create(27205, "tt1375666", 2010, 148, "https://example.com/poster.jpg");

        var emptyTitle = () => movie.AddOrUpdateTranslation("tr", " ", "Özet");
        emptyTitle.Should().Throw<ArgumentException>();

        var emptyOverview = () => movie.AddOrUpdateTranslation("tr", "Başlık", " ");
        emptyOverview.Should().Throw<ArgumentException>();

        movie.AddOrUpdateTranslation("tr", "Başlangıç", "İlk özet", "Tag 1");
        movie.AddOrUpdateTranslation("tr", "Başlangıç (Güncel)", "Yeni özet", "Tag 2");

        movie.Translations.Should().ContainSingle(t =>
            t.Title == "Başlangıç (Güncel)" && t.Overview == "Yeni özet" && t.Tagline == "Tag 2");

        movie.RemoveTranslation((LanguageCode)null!);
        movie.Translations.Should().ContainSingle();

        movie.RemoveTranslation("tr");
        movie.Translations.Should().BeEmpty();
    }

    [Fact]
    public void Delete_Should_SoftDeleteAssociatedPlatformsMoodsAndGenres()
    {
        var movie = Movie.Create(27205, "tt1375666", 2010, 148, "https://example.com/poster.jpg");
        movie.AddOrUpdateTranslation("tr", "Inception", "Overview");
        movie.AddPlatform(Guid.NewGuid(), "https://netflix.com/watch/1");
        movie.AddMood(Guid.NewGuid());
        movie.AddGenre(Guid.NewGuid());
        movie.ClearDomainEvents();

        movie.Delete("admin");

        movie.IsDeleted.Should().BeTrue();
        movie.MediaPlatforms.Should().AllSatisfy(p => p.IsDeleted.Should().BeTrue());
        movie.MediaMoods.Should().AllSatisfy(m => m.IsDeleted.Should().BeTrue());
        movie.MediaGenres.Should().AllSatisfy(g => g.IsDeleted.Should().BeTrue());
        movie.GetDomainEvents().Should().ContainSingle(e => e is MediaDeletedDomainEvent);
    }
}
