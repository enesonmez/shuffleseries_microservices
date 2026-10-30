using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Application.Common.Extensions;

public static class LocalizationExtensions
{
    public static MediaTranslation? GetLocalizedTranslation(this Media media, string? requestedLanguage)
        => ResolveTranslation(media.Translations, t => t.LanguageCode, requestedLanguage);

    public static EpisodeTranslation? GetLocalizedTranslation(this Episode episode, string? requestedLanguage)
        => ResolveTranslation(episode.Translations, t => t.LanguageCode, requestedLanguage);

    public static MoodTranslation? GetLocalizedTranslation(this Mood mood, string? requestedLanguage)
        => ResolveTranslation(mood.Translations, t => t.LanguageCode, requestedLanguage);

    public static GenreTranslation? GetLocalizedTranslation(this Genre genre, string? requestedLanguage)
        => ResolveTranslation(genre.Translations, t => t.LanguageCode, requestedLanguage);

    private static TTranslation? ResolveTranslation<TTranslation>(
        IReadOnlyCollection<TTranslation> translations,
        Func<TTranslation, LanguageCode> languageSelector,
        string? requestedLanguage)
        where TTranslation : class
    {
        if (translations.Count == 0)
        {
            return null;
        }

        // 1. Requested language match
        if (!string.IsNullOrWhiteSpace(requestedLanguage) &&
            LanguageCode.TryFrom(requestedLanguage, out var preferredCode))
        {
            var match = translations.FirstOrDefault(t => languageSelector(t) == preferredCode);
            if (match is not null)
            {
                return match;
            }
        }

        // 2. Graceful Fallback: Turkish -> English -> First available
        return translations.FirstOrDefault(t => languageSelector(t) == LanguageCode.Turkish)
            ?? translations.FirstOrDefault(t => languageSelector(t) == LanguageCode.English)
            ?? translations.FirstOrDefault();
    }
}
