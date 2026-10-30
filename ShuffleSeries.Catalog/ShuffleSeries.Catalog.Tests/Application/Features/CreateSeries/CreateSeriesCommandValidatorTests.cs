using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;
using ShuffleSeries.Catalog.Domain.Enums;

namespace ShuffleSeries.Catalog.Tests.Application.Features.CreateSeries;

public class CreateSeriesCommandValidatorTests
{
    private readonly CreateSeriesCommandValidator _validator = new();

    private static CreateSeriesCommand CreateValidCommand() => new(
        TmdbId: 1396,
        ImdbId: "tt0903747",
        ReleaseYear: 2008,
        SeriesType: SeriesType.Standard,
        PosterUrl: "https://example.com/breaking-bad.jpg",
        BackdropUrl: "https://example.com/bb-backdrop.jpg",
        LanguageCode: "tr",
        Title: "Breaking Bad",
        Overview: "A high school chemistry teacher turned methamphetamine producer.",
        Tagline: "Remember my name",
        ImdbRating: 9.5,
        ImdbVotes: 2000000,
        TotalSeasons: 5);

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithZeroOrNegativeTmdbId_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { TmdbId = 0 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.TmdbId)
              .WithErrorMessage("TMDb ID 0'dan büyük olmalıdır.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-imdb")]
    public void Validate_WithInvalidImdbId_ShouldHaveValidationError(string imdbId)
    {
        var command = CreateValidCommand() with { ImdbId = imdbId };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ImdbId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullTitle_ShouldHaveValidationError(string? title)
    {
        var command = CreateValidCommand() with { Title = title! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Başlık boş olamaz.");
    }

    [Fact]
    public void Validate_WithTitleExceeding250Characters_ShouldHaveValidationError()
    {
        var longTitle = new string('A', 251);
        var command = CreateValidCommand() with { Title = longTitle };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Başlık en fazla 250 karakter olabilir.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullOverview_ShouldHaveValidationError(string? overview)
    {
        var command = CreateValidCommand() with { Overview = overview! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Overview)
              .WithErrorMessage("Özet boş olamaz.");
    }

    [Fact]
    public void Validate_WithOverviewExceeding2000Characters_ShouldHaveValidationError()
    {
        var longOverview = new string('D', 2001);
        var command = CreateValidCommand() with { Overview = longOverview };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Overview)
              .WithErrorMessage("Özet en fazla 2000 karakter olabilir.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullLanguageCode_ShouldHaveValidationError(string? languageCode)
    {
        var command = CreateValidCommand() with { LanguageCode = languageCode! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.LanguageCode)
              .WithErrorMessage("Dil kodu boş olamaz.");
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2101)]
    public void Validate_WithInvalidReleaseYear_ShouldHaveValidationError(int invalidYear)
    {
        var command = CreateValidCommand() with { ReleaseYear = invalidYear };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ReleaseYear)
              .WithErrorMessage("Çıkış yılı geçerli bir yıl olmalıdır.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyPosterUrl_ShouldHaveValidationError(string? invalidPosterUrl)
    {
        var command = CreateValidCommand() with { PosterUrl = invalidPosterUrl! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PosterUrl)
              .WithErrorMessage("Afiş URL boş olamaz.");
    }

    [Theory]
    [InlineData("ftp://invalid-url")]
    [InlineData("not-a-url")]
    public void Validate_WithInvalidPosterUrlFormat_ShouldHaveValidationError(string invalidUrl)
    {
        var command = CreateValidCommand() with { PosterUrl = invalidUrl };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PosterUrl)
              .WithErrorMessage("Afiş URL geçerli bir HTTP veya HTTPS bağlantısı olmalıdır.");
    }

    [Theory]
    [InlineData("ftp://invalid-backdrop")]
    [InlineData("just-text")]
    public void Validate_WithInvalidBackdropUrl_ShouldHaveValidationError(string invalidUrl)
    {
        var command = CreateValidCommand() with { BackdropUrl = invalidUrl };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.BackdropUrl)
              .WithErrorMessage("Arka plan URL geçerli bir HTTP veya HTTPS bağlantısı olmalıdır.");
    }

    [Fact]
    public void Validate_WithInvalidSeriesType_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { SeriesType = (SeriesType)999 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.SeriesType)
              .WithErrorMessage("Geçersiz dizi türü.");
    }

    [Fact]
    public void Validate_WithNegativeTotalSeasons_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { TotalSeasons = -1 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.TotalSeasons)
              .WithErrorMessage("Toplam sezon sayısı negatif olamaz.");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(10.1)]
    public void Validate_WithInvalidImdbRating_ShouldHaveValidationError(double invalidRating)
    {
        var command = CreateValidCommand() with { ImdbRating = invalidRating };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ImdbRating)
              .WithErrorMessage("IMDb puanı 0 ile 10 arasında olmalıdır.");
    }

    [Fact]
    public void Validate_WithNegativeImdbVotes_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { ImdbVotes = -5 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ImdbVotes)
              .WithErrorMessage("IMDb oy sayısı 0 veya daha büyük olmalıdır.");
    }
}
