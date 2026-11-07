using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Events;

public sealed record EpisodeCreatedDomainEvent(
    Guid SeriesId,
    Guid SeasonId,
    Guid EpisodeId,
    int EpisodeNumber,
    bool IsStandalone) : IDomainEvent;
