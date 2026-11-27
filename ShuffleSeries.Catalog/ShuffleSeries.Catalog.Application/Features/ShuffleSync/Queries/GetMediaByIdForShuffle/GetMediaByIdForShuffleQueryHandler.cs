using ShuffleSeries.Catalog.Application.Features.ShuffleSync.Responses;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetMediaByIdForShuffle;

public sealed class GetMediaByIdForShuffleQueryHandler
{
    private readonly IMediaRepository _mediaRepository;

    public GetMediaByIdForShuffleQueryHandler(IMediaRepository mediaRepository)
    {
        _mediaRepository = mediaRepository;
    }

    public async Task<ShuffleMediaItemResponse> Handle(GetMediaByIdForShuffleQuery request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdForShuffleAsync(request.MediaId, cancellationToken)
            ?? throw new MediaNotFoundException(request.MediaId);

        return ShuffleMediaItemResponse.FromEntity(media);
    }
}
