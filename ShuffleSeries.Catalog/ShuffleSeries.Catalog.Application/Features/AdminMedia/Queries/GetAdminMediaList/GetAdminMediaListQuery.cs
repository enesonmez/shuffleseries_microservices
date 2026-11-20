using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Shared.Core.Application.Requests;

namespace ShuffleSeries.Catalog.Application.Features.AdminMedia.Queries.GetAdminMediaList;

public sealed record GetAdminMediaListQuery : PaginationRequest
{
    public MediaType? MediaType { get; init; }
    public string? Search { get; init; }
    public Guid? PlatformId { get; init; }
    public Guid? MoodId { get; init; }
    public Guid? GenreId { get; init; }
    public string? Language { get; init; }
    public bool IsDeleted { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = true;
}
