using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Catalog.Domain.Enums;

namespace ShuffleSeries.Catalog.Api.Contracts.Requests.AdminMedia;

public sealed class GetAdminMediaListRequest
{
    [FromQuery]
    public int? Page { get; init; }

    [FromQuery]
    public int? PageSize { get; init; }

    [FromQuery]
    public string? Search { get; init; }

    [FromQuery]
    public MediaType? MediaType { get; init; }

    [FromQuery]
    public Guid? PlatformId { get; init; }

    [FromQuery]
    public Guid? MoodId { get; init; }

    [FromQuery]
    public Guid? GenreId { get; init; }

    [FromQuery]
    public bool? IsDeleted { get; init; }

    [FromQuery]
    public string? SortBy { get; init; }

    [FromQuery]
    public bool? SortDescending { get; init; }
}
