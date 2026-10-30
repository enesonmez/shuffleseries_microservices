using ShuffleSeries.Catalog.Application.Common.Extensions;
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
}
