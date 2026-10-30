namespace ShuffleSeries.Catalog.Application.Features.Moods.Responses;

public sealed record MoodResponse(
    Guid Id,
    string Code,
    string AmbientColorHex,
    string? IconUrl,
    int DisplayOrder,
    bool IsActive,
    string Name,
    string Description,
    string? VibeTags,
    IReadOnlyList<MoodTranslationResponse> Translations);

public sealed record MoodTranslationResponse(
    string LanguageCode,
    string Name,
    string Description,
    string? VibeTags);
