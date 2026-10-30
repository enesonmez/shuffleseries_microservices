using MediatR;
using ShuffleSeries.Catalog.Application.Features.AdminMedia.Responses;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Shared.Core.Application.Requests;
using ShuffleSeries.Shared.Core.Application.Responses;

namespace ShuffleSeries.Catalog.Application.Features.AdminMedia.Queries.GetAdminMediaList;

public sealed record GetAdminMediaListQuery : PaginationRequest, IRequest<PaginatedList<AdminMediaListItemResponse>>
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

    public GetAdminMediaListQuery(
        MediaType? mediaType = null,
        string? search = null,
        Guid? platformId = null,
        Guid? moodId = null,
        Guid? genreId = null,
        int? pageNumber = 1,
        int? pageSize = 10,
        string? language = null,
        bool isDeleted = false,
        string? sortBy = null,
        bool sortDescending = true) : base(pageNumber, pageSize)
    {
        MediaType = mediaType;
        Search = search;
        PlatformId = platformId;
        MoodId = moodId;
        GenreId = genreId;
        Language = language;
        IsDeleted = isDeleted;
        SortBy = sortBy;
        SortDescending = sortDescending;
    }
}
