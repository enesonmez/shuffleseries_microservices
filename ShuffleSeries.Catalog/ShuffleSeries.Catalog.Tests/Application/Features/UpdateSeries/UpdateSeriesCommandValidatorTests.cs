using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;
using ShuffleSeries.Catalog.Domain.Enums;

namespace ShuffleSeries.Catalog.Tests.Application.Features.UpdateSeries;

public class UpdateSeriesCommandValidatorTests
{
    private readonly UpdateSeriesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var command = new UpdateSeriesCommand(Guid.NewGuid(), 2015, SeriesType.Standard, 6);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyId_ShouldHaveValidationError()
    {
        var command = new UpdateSeriesCommand(Guid.Empty, 2015, SeriesType.Standard, 6);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Dizi ID boş olamaz.");
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2101)]
    public void Validate_WithInvalidReleaseYear_ShouldHaveValidationError(int invalidYear)
    {
        var command = new UpdateSeriesCommand(Guid.NewGuid(), invalidYear, SeriesType.Standard, 6);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ReleaseYear)
              .WithErrorMessage("Çıkış yılı geçerli bir yıl olmalıdır.");
    }

    [Fact]
    public void Validate_WithInvalidSeriesType_ShouldHaveValidationError()
    {
        var command = new UpdateSeriesCommand(Guid.NewGuid(), 2015, (SeriesType)999, 6);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.SeriesType)
              .WithErrorMessage("Geçersiz dizi türü.");
    }

    [Fact]
    public void Validate_WithNegativeTotalSeasons_ShouldHaveValidationError()
    {
        var command = new UpdateSeriesCommand(Guid.NewGuid(), 2015, SeriesType.Standard, -1);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.TotalSeasons)
              .WithErrorMessage("Toplam sezon sayısı negatif olamaz.");
    }

    [Fact]
    public void Validate_WithImdbIdExceeding20Chars_ShouldHaveValidationError()
    {
        var command = new UpdateSeriesCommand(Guid.NewGuid(), 2015, SeriesType.Standard, 6, ImdbId: new string('A', 21));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ImdbId)
              .WithErrorMessage("IMDb ID en fazla 20 karakter olabilir.");
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(10.5)]
    public void Validate_WithInvalidImdbRating_ShouldHaveValidationError(double invalidRating)
    {
        var command = new UpdateSeriesCommand(Guid.NewGuid(), 2015, SeriesType.Standard, 6, ImdbRating: invalidRating);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ImdbRating)
              .WithErrorMessage("IMDb puanı 0 ile 10 arasında olmalıdır.");
    }

    [Fact]
    public void Validate_WithNegativeImdbVotes_ShouldHaveValidationError()
    {
        var command = new UpdateSeriesCommand(Guid.NewGuid(), 2015, SeriesType.Standard, 6, ImdbVotes: -10);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ImdbVotes)
              .WithErrorMessage("IMDb oy sayısı 0 veya daha büyük olmalıdır.");
    }
}
