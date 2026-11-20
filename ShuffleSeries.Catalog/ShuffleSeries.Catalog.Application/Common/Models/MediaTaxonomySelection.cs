namespace ShuffleSeries.Catalog.Application.Common.Models;

public sealed record MediaTaxonomySelection(
    IReadOnlyList<Guid>? PlatformIds,
    IReadOnlyList<Guid>? MoodIds,
    IReadOnlyList<Guid>? GenreIds);
