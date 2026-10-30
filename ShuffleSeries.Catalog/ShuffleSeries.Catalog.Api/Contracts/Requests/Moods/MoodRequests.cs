namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Moods;

public sealed record CreateMoodRequest(
    string Code,
    string AmbientColorHex,
    string Name,
    string Description,
    string LanguageCode,
    string? VibeTags = null,
    string? IconUrl = null,
    int DisplayOrder = 0,
    bool IsActive = true);

public sealed record UpdateMoodRequest(
    string Code,
    string AmbientColorHex,
    string? IconUrl = null,
    int DisplayOrder = 0);

public sealed record AddOrUpdateMoodTranslationRequest(
    string LanguageCode,
    string Name,
    string Description,
    string? VibeTags = null);
