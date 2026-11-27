using ShuffleSeries.Catalog.Application.Features.ShuffleSync.Responses;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Responses;

namespace ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetBulkMediaForShuffle;

public sealed class GetBulkMediaForShuffleQueryHandler
{
    private readonly IMediaRepository _mediaRepository;

    public GetBulkMediaForShuffleQueryHandler(IMediaRepository mediaRepository)
    {
        _mediaRepository = mediaRepository;
    }

    public async Task<PaginatedList<ShuffleMediaItemResponse>> Handle(GetBulkMediaForShuffleQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _mediaRepository.GetBulkForShuffleAsync(
            request.MediaType,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var mappedItems = items
            .Select(ShuffleMediaItemResponse.FromEntity)
            .ToList();

        return new PaginatedList<ShuffleMediaItemResponse>(
            mappedItems,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
