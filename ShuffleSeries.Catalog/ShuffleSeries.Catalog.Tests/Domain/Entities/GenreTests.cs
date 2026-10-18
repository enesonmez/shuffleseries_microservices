using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Events;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Tests.Domain.Entities;

public class GenreTests
{
    [Fact]
    public void Create_Should_CreateGenreAndRaiseEvent()
    {
        // Act
        var genre = Genre.Create("science-fiction", 1);

        // Assert
        genre.Should().NotBeNull();
        genre.Id.Should().NotBeEmpty();
        genre.Code.Should().Be("science-fiction");
        genre.DisplayOrder.Should().Be(1);
        genre.IsActive.Should().BeTrue();

        genre.GetDomainEvents().Should().ContainSingle(e => e is GenreCreatedDomainEvent);
    }

    [Fact]
    public void AddOrUpdateTranslation_Should_AddAndNormalizeLanguageCode()
    {
        // Arrange
        var genre = Genre.Create("science-fiction", 1);

        // Act
        var tr = genre.AddOrUpdateTranslation("TR", "Bilim Kurgu", "Gelecek ve uzay");

        // Assert
        tr.LanguageCode.Should().Be(LanguageCode.Turkish);
        tr.Name.Should().Be("Bilim Kurgu");
        tr.Description.Should().Be("Gelecek ve uzay");
        genre.Translations.Should().Contain(tr);
    }

    [Fact]
    public void AddOrUpdateTranslation_Should_UpdateExistingTranslation()
    {
        // Arrange
        var genre = Genre.Create("action", 1);
        genre.AddOrUpdateTranslation(LanguageCode.Turkish, "Aksiyon", "Eski açıklama");

        // Act
        var updated = genre.AddOrUpdateTranslation(LanguageCode.Turkish, "Aksiyon 2", "Yeni açıklama");

        // Assert
        updated.Name.Should().Be("Aksiyon 2");
        updated.Description.Should().Be("Yeni açıklama");
        genre.Translations.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveTranslation_Should_RemoveTranslation()
    {
        // Arrange
        var genre = Genre.Create("action", 1);
        genre.AddOrUpdateTranslation(LanguageCode.Turkish, "Aksiyon");
        genre.AddOrUpdateTranslation(LanguageCode.English, "Action");

        // Act
        genre.RemoveTranslation("tr");

        // Assert
        genre.Translations.Should().HaveCount(1);
        genre.Translations.Should().NotContain(t => t.LanguageCode == LanguageCode.Turkish);
    }

    [Fact]
    public void Update_Should_UpdateDetailsAndRaiseEvent()
    {
        // Arrange
        var genre = Genre.Create("code", 1);
        genre.ClearDomainEvents();

        // Act
        genre.Update("new-code", 5);

        // Assert
        genre.Code.Should().Be("new-code");
        genre.DisplayOrder.Should().Be(5);
        genre.ModifiedAtUtc.Should().NotBeNull();

        genre.GetDomainEvents().Should().ContainSingle(e => e is GenreUpdatedDomainEvent);
    }

    [Fact]
    public void ToggleActive_Should_ToggleAndRaiseEvent()
    {
        // Arrange
        var genre = Genre.Create("sci-fi", 1);
        genre.ClearDomainEvents();

        // Act
        genre.ToggleActive(false);

        // Assert
        genre.IsActive.Should().BeFalse();
        genre.GetDomainEvents().Should().ContainSingle(e => e is GenreUpdatedDomainEvent);
    }

    [Fact]
    public void Delete_Should_SoftDeleteAndRaiseEvent()
    {
        // Arrange
        var genre = Genre.Create("sci-fi", 1);
        genre.ClearDomainEvents();

        // Act
        genre.Delete("admin");

        // Assert
        genre.IsDeleted.Should().BeTrue();
        genre.DeletedBy.Should().Be("admin");
        genre.DeletedAtUtc.Should().NotBeNull();
        genre.GetDomainEvents().Should().ContainSingle(e => e is GenreDeletedDomainEvent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowArgumentException_WhenCodeIsEmpty(string code)
    {
        var action = () => Genre.Create(code, 1);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Media_AddGenre_Should_AddMediaGenre()
    {
        // Arrange
        var movie = Movie.Create(2020, 120, "https://poster.jpg", null, "tt123", 8.0, 1000, "Director");
        var genreId = Guid.NewGuid();

        // Act
        var mediaGenre = movie.AddGenre(genreId);

        // Assert
        mediaGenre.Should().NotBeNull();
        mediaGenre.GenreId.Should().Be(genreId);
        movie.MediaGenres.Should().Contain(mediaGenre);
    }

    [Fact]
    public void Media_AddGenre_ShouldThrowDuplicateMediaGenreException_WhenAlreadyAdded()
    {
        // Arrange
        var movie = Movie.Create(2020, 120, "https://poster.jpg", null, "tt123", 8.0, 1000, "Director");
        var genreId = Guid.NewGuid();
        movie.AddGenre(genreId);

        // Act
        var action = () => movie.AddGenre(genreId);

        // Assert
        action.Should().Throw<DuplicateMediaGenreException>();
    }

    [Fact]
    public void Media_RemoveGenre_Should_RemoveMediaGenre()
    {
        // Arrange
        var movie = Movie.Create(2020, 120, "https://poster.jpg", null, "tt123", 8.0, 1000, "Director");
        var genreId = Guid.NewGuid();
        movie.AddGenre(genreId);

        // Act
        movie.RemoveGenre(genreId);

        // Assert
        movie.MediaGenres.Should().BeEmpty();
    }
}
