using ShuffleSeries.Catalog.Application.Features.ShuffleSync.Responses;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Responses;

namespace ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetStandaloneEpisodesForRoulette;

public sealed class GetStandaloneEpisodesForRouletteQueryHandler
{
    private readonly IEpisodeRepository _episodeRepository;

    public GetStandaloneEpisodesForRouletteQueryHandler(IEpisodeRepository episodeRepository)
    {
        _episodeRepository = episodeRepository;
    }

    public async Task<PaginatedList<ShuffleStandaloneEpisodeResponse>> Handle(
        GetStandaloneEpisodesForRouletteQuery request,
        CancellationToken cancellationToken)
    {
        var (episodes, totalCount) = await _episodeRepository.GetStandaloneEpisodesWithSeriesContextAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var mappedItems = episodes
            .Select(ShuffleStandaloneEpisodeResponse.FromContext)
            .ToList();

        return new PaginatedList<ShuffleStandaloneEpisodeResponse>(
            mappedItems,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
