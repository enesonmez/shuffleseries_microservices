using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Shared.Core.Application.Requests;

namespace ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetBulkMediaForShuffle;

public sealed record GetBulkMediaForShuffleQuery : PaginationRequest
{
    public MediaType? MediaType { get; init; }

    public GetBulkMediaForShuffleQuery()
    {
    }

    public GetBulkMediaForShuffleQuery(int? pageNumber, int? pageSize, MediaType? mediaType = null)
        : base(pageNumber, pageSize)
    {
        MediaType = mediaType;
    }
}
