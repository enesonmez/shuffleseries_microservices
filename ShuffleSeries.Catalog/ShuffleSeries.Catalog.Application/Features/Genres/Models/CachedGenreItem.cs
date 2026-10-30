namespace ShuffleSeries.Catalog.Application.Features.Genres.Models;

/// <summary>
/// Dağıtık önbellekte (Redis) saklanan, tüm çevirileri barındıran serileştirilebilir tür modeli.
/// </summary>
public sealed record CachedGenreItem(
    Guid Id,
    string Code,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<CachedGenreTranslationItem> Translations);

/// <summary>
/// Dağıtık önbellekte saklanan tür çeviri öğesi.
/// </summary>
public sealed record CachedGenreTranslationItem(
    string LanguageCode,
    string Name,
    string? Description);
