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
