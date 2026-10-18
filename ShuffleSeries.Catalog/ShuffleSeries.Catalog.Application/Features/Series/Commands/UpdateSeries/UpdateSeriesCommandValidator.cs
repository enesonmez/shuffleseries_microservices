using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;

public sealed class UpdateSeriesCommandValidator : AbstractValidator<UpdateSeriesCommand>
{
    public UpdateSeriesCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Dizi ID boş olamaz.");

        RuleFor(x => x.ReleaseYear)
            .InclusiveBetween(1900, 2100).WithMessage("Çıkış yılı geçerli bir yıl olmalıdır.");

        RuleFor(x => x.SeriesType)
            .IsInEnum().WithMessage("Geçersiz dizi türü.");

        RuleFor(x => x.TotalSeasons)
            .GreaterThanOrEqualTo(0).WithMessage("Toplam sezon sayısı negatif olamaz.");

        RuleFor(x => x.ImdbId)
            .MaximumLength(20).WithMessage("IMDb ID en fazla 20 karakter olabilir.");

        RuleFor(x => x.ImdbRating)
            .InclusiveBetween(0.0, 10.0).When(x => x.ImdbRating.HasValue)
            .WithMessage("IMDb puanı 0 ile 10 arasında olmalıdır.");

        RuleFor(x => x.ImdbVotes)
            .GreaterThanOrEqualTo(0).When(x => x.ImdbVotes.HasValue)
            .WithMessage("IMDb oy sayısı 0 veya daha büyük olmalıdır.");
    }
}
