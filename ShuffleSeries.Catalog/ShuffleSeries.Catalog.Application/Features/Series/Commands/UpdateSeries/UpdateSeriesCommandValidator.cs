using FluentValidation;
using ShuffleSeries.Shared.Core.Application.Validators;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;

public sealed class UpdateSeriesCommandValidator : AbstractValidator<UpdateSeriesCommand>
{
    public UpdateSeriesCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Dizi ID boş olamaz.");

        RuleFor(x => x.TmdbId)
            .GreaterThan(0).WithMessage("TMDb ID 0'dan büyük olmalıdır.");

        RuleFor(x => x.ImdbId)
            .NotEmpty().WithMessage("IMDb ID boş olamaz.")
            .Matches(@"^tt\d{7,10}$").WithMessage("IMDb ID geçerli formatta olmalıdır (örn. 'tt0111161').");

        RuleFor(x => x.ReleaseYear)
            .InclusiveBetween(1900, 2100).WithMessage("Çıkış yılı geçerli bir yıl olmalıdır.");

        RuleFor(x => x.SeriesType)
            .IsInEnum().WithMessage("Geçersiz dizi türü.");

        RuleFor(x => x.TotalSeasons)
            .GreaterThanOrEqualTo(0).WithMessage("Toplam sezon sayısı negatif olamaz.");

        RuleFor(x => x.PosterUrl)
            .MaximumLength(500).WithMessage("Afiş URL en fazla 500 karakter olabilir.")
            .MustBeHttpUrl().WithMessage("Afiş URL geçerli bir HTTP veya HTTPS bağlantısı olmalıdır.")
            .When(x => !string.IsNullOrEmpty(x.PosterUrl));

        RuleFor(x => x.BackdropUrl)
            .MaximumLength(500).WithMessage("Arka plan URL en fazla 500 karakter olabilir.")
            .MustBeHttpUrl().WithMessage("Arka plan URL geçerli bir HTTP veya HTTPS bağlantısı olmalıdır.")
            .When(x => !string.IsNullOrEmpty(x.BackdropUrl));

        RuleFor(x => x.ImdbRating)
            .InclusiveBetween(0.0, 10.0).When(x => x.ImdbRating.HasValue)
            .WithMessage("IMDb puanı 0 ile 10 arasında olmalıdır.");

        RuleFor(x => x.ImdbVotes)
            .GreaterThanOrEqualTo(0).When(x => x.ImdbVotes.HasValue)
            .WithMessage("IMDb oy sayısı 0 veya daha büyük olmalıdır.");
    }
}
