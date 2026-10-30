using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.UpdateEpisode;

public sealed record UpdateEpisodeCommand(
    Guid Id,
    int EpisodeNumber,
    bool IsStandalone,
    int? RuntimeMinutes = null,
    double? ImdbRating = null,
    string? ThumbnailUrl = null) : IRequest;
