using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Events;

public sealed record EpisodeCreatedDomainEvent(
    Guid SeriesId,
    Guid SeasonId,
    Guid EpisodeId,
    int EpisodeNumber,
    bool IsStandalone) : IDomainEvent;

public sealed record EpisodeUpdatedDomainEvent(
    Guid EpisodeId,
    int EpisodeNumber,
    bool IsStandalone,
    int? RuntimeMinutes,
    double? ImdbRating) : IDomainEvent;

public sealed record EpisodeDeletedDomainEvent(
    Guid EpisodeId) : IDomainEvent;
