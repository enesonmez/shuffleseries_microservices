namespace ShuffleSeries.Catalog.Application.Features.Episodes.Queries.GetEpisodeById;

public sealed record GetEpisodeByIdQuery(Guid Id, string? Language = null);
