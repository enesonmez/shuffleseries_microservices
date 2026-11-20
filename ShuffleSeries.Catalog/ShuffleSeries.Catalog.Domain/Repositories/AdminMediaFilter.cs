using ShuffleSeries.Catalog.Domain.Enums;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public sealed record AdminMediaFilter
{
    public MediaType? MediaType { get; init; }
    public string? Search { get; init; }
    public Guid? PlatformId { get; init; }
    public Guid? MoodId { get; init; }
    public Guid? GenreId { get; init; }
    public bool IsDeleted { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = true;
    public string? TargetLanguage { get; init; }
}
