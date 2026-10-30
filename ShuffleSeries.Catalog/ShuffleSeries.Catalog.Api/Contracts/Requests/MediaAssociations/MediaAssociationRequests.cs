namespace ShuffleSeries.Catalog.Api.Contracts.Requests.MediaAssociations;

public sealed record AddMediaPlatformRequest(
    Guid PlatformId,
    string WebUrl,
    string? AppSchemeUrl = null);

public sealed record AddMediaMoodRequest(
    Guid MoodId);

public sealed record AddMediaGenreRequest(
    Guid GenreId);

public sealed record AddOrUpdateTranslationRequest(
    string LanguageCode,
    string Title,
    string Overview,
    string? Tagline = null);
