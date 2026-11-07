namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Moods;

public sealed record AddOrUpdateMoodTranslationRequest(
    string LanguageCode,
    string Name,
    string Description,
    string? VibeTags = null);
