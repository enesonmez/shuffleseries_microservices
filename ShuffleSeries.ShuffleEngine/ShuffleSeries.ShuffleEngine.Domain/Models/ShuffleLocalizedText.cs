namespace ShuffleSeries.ShuffleEngine.Domain.Models;

public sealed record ShuffleLocalizedText(
    string Title,
    string? Overview,
    string? Tagline = null);
