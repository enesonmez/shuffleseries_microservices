using ShuffleSeries.Shared.Core.Application.Requests;

namespace ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetStandaloneEpisodesForRoulette;

public sealed record GetStandaloneEpisodesForRouletteQuery : PaginationRequest
{
    public Guid? SeriesId { get; init; }

    public GetStandaloneEpisodesForRouletteQuery()
    {
    }

    public GetStandaloneEpisodesForRouletteQuery(int? pageNumber, int? pageSize, Guid? seriesId = null)
        : base(pageNumber, pageSize)
    {
        SeriesId = seriesId;
    }
}
