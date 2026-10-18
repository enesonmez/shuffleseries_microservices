using FluentValidation;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;

public sealed class CreateSeriesCommandValidator : AbstractValidator<CreateSeriesCommand>
{
    public CreateSeriesCommandValidator()
    {
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
            .MaximumLength(500).WithMessage("Afiş URL en fazla 500 karakter olabilir.");

        RuleFor(x => x.PosterUrl)
            .Must(BeAValidHttpUrl).When(x => !string.IsNullOrWhiteSpace(x.PosterUrl))
            .WithMessage("Afiş URL geçerli bir HTTP veya HTTPS bağlantısı olmalıdır.");

        RuleFor(x => x.BackdropUrl)
            .MaximumLength(500).WithMessage("Arka plan URL en fazla 500 karakter olabilir.")
            .Must(BeAValidHttpUrl).When(x => !string.IsNullOrWhiteSpace(x.BackdropUrl))
            .WithMessage("Arka plan URL geçerli bir HTTP veya HTTPS bağlantısı olmalıdır.");

        RuleFor(x => x.ReleaseYear)
            .InclusiveBetween(1900, 2100).WithMessage("Çıkış yılı geçerli bir yıl olmalıdır.");

        RuleFor(x => x.SeriesType)
            .IsInEnum().WithMessage("Geçersiz dizi türü.");

        RuleFor(x => x.TotalSeasons)
            .GreaterThanOrEqualTo(0).WithMessage("Toplam sezon sayısı negatif olamaz.");

        RuleFor(x => x.Tagline)
            .MaximumLength(500).WithMessage("Slogan en fazla 500 karakter olabilir.");

        RuleFor(x => x.ImdbId)
            .MaximumLength(20).WithMessage("IMDb ID en fazla 20 karakter olabilir.");

        RuleFor(x => x.ImdbRating)
            .InclusiveBetween(0.0, 10.0).When(x => x.ImdbRating.HasValue)
            .WithMessage("IMDb puanı 0 ile 10 arasında olmalıdır.");

        RuleFor(x => x.ImdbVotes)
            .GreaterThanOrEqualTo(0).When(x => x.ImdbVotes.HasValue)
            .WithMessage("IMDb oy sayısı 0 veya daha büyük olmalıdır.");
    }

    private static bool BeAValidHttpUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
