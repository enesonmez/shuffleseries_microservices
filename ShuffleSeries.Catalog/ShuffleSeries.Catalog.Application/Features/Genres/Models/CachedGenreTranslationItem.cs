namespace ShuffleSeries.Catalog.Application.Features.Genres.Models;

/// <summary>
/// Dağıtık önbellekte saklanan tür çeviri öğesi.
/// </summary>
public sealed record CachedGenreTranslationItem(
    string LanguageCode,
    string Name,
    string? Description);
