using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public interface IEpisodeRepository : IRepository
{
    Task<Episode?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Episode>> GetStandaloneEpisodesAsync(int? limit = null, CancellationToken cancellationToken = default);
}
