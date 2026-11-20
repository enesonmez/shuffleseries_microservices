namespace ShuffleSeries.Catalog.Application.Features.Moods.Models;

/// <summary>
/// Dağıtık önbellekte (Redis) saklanan, tüm çevirileri barındıran serileştirilebilir ruh hali modeli.
/// </summary>
public sealed record CachedMoodItem(
    Guid Id,
    string Code,
    string AmbientColorHex,
    string? IconUrl,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<CachedMoodTranslationItem> Translations);
