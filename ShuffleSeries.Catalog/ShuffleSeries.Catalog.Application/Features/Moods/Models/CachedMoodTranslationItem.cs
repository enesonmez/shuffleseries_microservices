namespace ShuffleSeries.Catalog.Application.Features.Moods.Models;

/// <summary>
/// Dağıtık önbellekte saklanan ruh hali çeviri öğesi.
/// </summary>
public sealed record CachedMoodTranslationItem(
    string LanguageCode,
    string Name,
    string Description,
    string? VibeTags);
