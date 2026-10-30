using ShuffleSeries.Catalog.Application.Common.Extensions;
using ShuffleSeries.Catalog.Application.Features.Genres.Models;
using ShuffleSeries.Catalog.Application.Features.Moods.Models;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Tests.Application.Common;

public class LocalizationExtensionsTests
{
    [Fact]
    public void GetLocalizedTranslation_Should_ReturnRequestedLanguage_When_Present()
    {
        // Arrange
        var movie = Movie.Create(
            tmdbId: 550,
            imdbId: "tt0137523",
            releaseYear: 2020,
            runtimeMinutes: 120,
            posterUrl: "https://example.com/poster.jpg");

        movie.AddOrUpdateTranslation(LanguageCode.Turkish, "Tenet TR", "Tenet TR Özet");
        movie.AddOrUpdateTranslation(LanguageCode.English, "Tenet EN", "Tenet EN Overview");

        // Act
        var result = movie.GetLocalizedTranslation("en");

        // Assert
        result.Should().NotBeNull();
        result!.LanguageCode.Should().Be(LanguageCode.English);
        result.Title.Should().Be("Tenet EN");
    }

    [Fact]
    public void GetLocalizedTranslation_Should_FallbackToTurkish_When_RequestedLanguageMissing()
    {
        // Arrange
        var movie = Movie.Create(
            tmdbId: 550,
            imdbId: "tt0137523",
            releaseYear: 2020,
            runtimeMinutes: 120,
            posterUrl: "https://example.com/poster.jpg");

        movie.AddOrUpdateTranslation(LanguageCode.Turkish, "Tenet TR", "Tenet TR Özet");

        // Act - Requesting German ('de') which doesn't exist
        var result = movie.GetLocalizedTranslation("de");

        // Assert
        result.Should().NotBeNull();
        result!.LanguageCode.Should().Be(LanguageCode.Turkish);
        result.Title.Should().Be("Tenet TR");
    }

    [Fact]
    public void GetLocalizedTranslation_Should_FallbackToFirstAvailable_When_TurkishAlsoMissing()
    {
        // Arrange
        var movie = Movie.Create(
            tmdbId: 550,
            imdbId: "tt0137523",
            releaseYear: 2020,
            runtimeMinutes: 120,
            posterUrl: "https://example.com/poster.jpg");

        movie.AddOrUpdateTranslation(LanguageCode.English, "Tenet EN", "Tenet EN Overview");

        // Act - Requesting French ('fr')
        var result = movie.GetLocalizedTranslation("fr");

        // Assert
        result.Should().NotBeNull();
        result!.LanguageCode.Should().Be(LanguageCode.English);
        result.Title.Should().Be("Tenet EN");
    }

    [Fact]
    public void GetLocalizedTranslation_CachedMood_Should_ReturnRequestedLanguage_When_Present()
    {
        // Arrange
        var cachedMood = new CachedMoodItem(
            Guid.NewGuid(),
            "chill",
            "#123456",
            "https://example.com/icon.png",
            1,
            true,
            new List<CachedMoodTranslationItem>
            {
                new("tr", "Sakin", "Sakin mod", "rahat,huzurlu"),
                new("en", "Chill", "Chill mode", "relaxed,peaceful")
            });

        // Act
        var result = cachedMood.GetLocalizedTranslation("en");

        // Assert
        result.Should().NotBeNull();
        result!.LanguageCode.Should().Be("en");
        result.Name.Should().Be("Chill");
        result.Description.Should().Be("Chill mode");
    }

    [Fact]
    public void GetLocalizedTranslation_CachedMood_Should_FallbackToTurkish_When_RequestedLanguageMissing()
    {
        // Arrange
        var cachedMood = new CachedMoodItem(
            Guid.NewGuid(),
            "chill",
            "#123456",
            "https://example.com/icon.png",
            1,
            true,
            new List<CachedMoodTranslationItem>
            {
                new("tr", "Sakin", "Sakin mod", "rahat,huzurlu")
            });

        // Act
        var result = cachedMood.GetLocalizedTranslation("de");

        // Assert
        result.Should().NotBeNull();
        result!.LanguageCode.Should().Be("tr");
        result.Name.Should().Be("Sakin");
    }

    [Fact]
    public void GetLocalizedTranslation_CachedGenre_Should_ReturnRequestedLanguage_When_Present()
    {
        // Arrange
        var cachedGenre = new CachedGenreItem(
            Guid.NewGuid(),
            "action",
            1,
            true,
            new List<CachedGenreTranslationItem>
            {
                new("tr", "Aksiyon", "Aksiyon açıklaması"),
                new("en", "Action", "Action description")
            });

        // Act
        var result = cachedGenre.GetLocalizedTranslation("en");

        // Assert
        result.Should().NotBeNull();
        result!.LanguageCode.Should().Be("en");
        result.Name.Should().Be("Action");
    }

    [Fact]
    public void GetLocalizedTranslation_CachedGenre_Should_FallbackToFirstAvailable_When_TurkishAndRequestedMissing()
    {
        // Arrange
        var cachedGenre = new CachedGenreItem(
            Guid.NewGuid(),
            "action",
            1,
            true,
            new List<CachedGenreTranslationItem>
            {
                new("fr", "Action FR", "Description FR")
            });

        // Act
        var result = cachedGenre.GetLocalizedTranslation("de");

        // Assert
        result.Should().NotBeNull();
        result!.LanguageCode.Should().Be("fr");
        result.Name.Should().Be("Action FR");
    }
}
