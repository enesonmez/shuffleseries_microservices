using FluentValidation;
using ShuffleSeries.Shared.Core.Application.Validators;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;

public sealed class CreateSeriesCommandValidator : AbstractValidator<CreateSeriesCommand>
{
    public CreateSeriesCommandValidator()
    {
        RuleFor(x => x.TmdbId)
            .GreaterThan(0).WithMessage("TMDb ID 0'dan büyük olmalıdır.");

        RuleFor(x => x.ImdbId)
            .NotEmpty().WithMessage("IMDb ID boş olamaz.")
            .Matches(@"^tt\d{7,10}$").WithMessage("IMDb ID geçerli formatta olmalıdır (örn. 'tt0111161').");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık boş olamaz.")
            .MaximumLength(250).WithMessage("Başlık en fazla 250 karakter olabilir.");

        RuleFor(x => x.Overview)
            .NotEmpty().WithMessage("Özet boş olamaz.")
            .MaximumLength(2000).WithMessage("Özet en fazla 2000 karakter olabilir.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu boş olamaz.")
            .MaximumLength(10).WithMessage("Dil kodu en fazla 10 karakter olabilir.")
            .Must(code => LanguageCode.IsSupported(code))
            .WithMessage(x => $"Desteklenmeyen dil kodu '{x.LanguageCode}'. Desteklenen diller: {string.Join(", ", LanguageCode.SupportedValues)}");

        RuleFor(x => x.PosterUrl)
            .NotEmpty().WithMessage("Afiş URL boş olamaz.")
            .MaximumLength(500).WithMessage("Afiş URL en fazla 500 karakter olabilir.")
            .MustBeHttpUrl().WithMessage("Afiş URL geçerli bir HTTP veya HTTPS bağlantısı olmalıdır.");

        RuleFor(x => x.BackdropUrl)
            .MaximumLength(500).WithMessage("Arka plan URL en fazla 500 karakter olabilir.")
            .MustBeHttpUrl().WithMessage("Arka plan URL geçerli bir HTTP veya HTTPS bağlantısı olmalıdır.");

        RuleFor(x => x.ReleaseYear)
            .InclusiveBetween(1900, 2100).WithMessage("Çıkış yılı geçerli bir yıl olmalıdır.");

        RuleFor(x => x.SeriesType)
            .IsInEnum().WithMessage("Geçersiz dizi türü.");

        RuleFor(x => x.TotalSeasons)
            .GreaterThanOrEqualTo(0).WithMessage("Toplam sezon sayısı negatif olamaz.");

        RuleFor(x => x.Tagline)
            .MaximumLength(500).WithMessage("Slogan en fazla 500 karakter olabilir.");

        RuleFor(x => x.ImdbRating)
            .InclusiveBetween(0.0, 10.0).When(x => x.ImdbRating.HasValue)
            .WithMessage("IMDb puanı 0 ile 10 arasında olmalıdır.");

        RuleFor(x => x.ImdbVotes)
            .GreaterThanOrEqualTo(0).When(x => x.ImdbVotes.HasValue)
            .WithMessage("IMDb oy sayısı 0 veya daha büyük olmalıdır.");

        RuleForEach(x => x.PlatformIds)
            .NotEmpty().WithMessage("Platform ID boş olamaz.");

        RuleForEach(x => x.MoodIds)
            .NotEmpty().WithMessage("Mood ID boş olamaz.");

        RuleForEach(x => x.GenreIds)
            .NotEmpty().WithMessage("Genre ID boş olamaz.");
    }
}
