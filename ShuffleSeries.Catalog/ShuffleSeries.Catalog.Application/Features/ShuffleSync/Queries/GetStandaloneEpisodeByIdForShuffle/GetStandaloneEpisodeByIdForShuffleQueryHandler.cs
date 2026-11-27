using ShuffleSeries.Catalog.Application.Features.ShuffleSync.Responses;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetStandaloneEpisodeByIdForShuffle;

public sealed class GetStandaloneEpisodeByIdForShuffleQueryHandler
{
    private readonly IEpisodeRepository _episodeRepository;

    public GetStandaloneEpisodeByIdForShuffleQueryHandler(IEpisodeRepository episodeRepository)
    {
        _episodeRepository = episodeRepository;
    }

    public async Task<ShuffleStandaloneEpisodeResponse> Handle(
        GetStandaloneEpisodeByIdForShuffleQuery request,
        CancellationToken cancellationToken)
    {
        var context = await _episodeRepository.GetStandaloneEpisodeByIdWithSeriesContextAsync(
            request.EpisodeId,
            cancellationToken);

        if (context is null)
        {
            throw new EpisodeNotFoundException(request.EpisodeId);
        }

        return ShuffleStandaloneEpisodeResponse.FromContext(context);
    }
}
