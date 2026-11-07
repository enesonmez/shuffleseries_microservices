namespace ShuffleSeries.Catalog.Application.Features.Moods.Responses;

public sealed record MoodTranslationResponse(
    string LanguageCode,
    string Name,
    string Description,
    string? VibeTags);
