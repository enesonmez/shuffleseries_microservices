using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Events;

public sealed record EpisodeUpdatedDomainEvent(
    Guid EpisodeId,
    int EpisodeNumber,
    bool IsStandalone,
    int? RuntimeMinutes,
    double? ImdbRating) : IDomainEvent;
