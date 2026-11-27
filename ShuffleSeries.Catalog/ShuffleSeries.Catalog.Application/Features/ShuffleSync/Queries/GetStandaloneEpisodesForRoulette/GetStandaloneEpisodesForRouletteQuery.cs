using ShuffleSeries.Shared.Core.Application.Requests;

namespace ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetStandaloneEpisodesForRoulette;

public sealed record GetStandaloneEpisodesForRouletteQuery : PaginationRequest
{
    public GetStandaloneEpisodesForRouletteQuery()
    {
    }

    public GetStandaloneEpisodesForRouletteQuery(int? pageNumber, int? pageSize)
        : base(pageNumber, pageSize)
    {
    }
}
