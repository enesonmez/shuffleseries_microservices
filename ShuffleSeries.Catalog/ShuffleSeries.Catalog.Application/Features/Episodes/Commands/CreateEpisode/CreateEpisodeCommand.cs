using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.CreateEpisode;

public sealed record CreateEpisodeCommand(
    Guid SeriesId,
    int SeasonNumber,
    int EpisodeNumber,
    string Title,
    string? Overview,
    string LanguageCode,
    bool? IsStandalone = null,
    int? RuntimeMinutes = null,
    double? ImdbRating = null,
    string? ThumbnailUrl = null) : IRequest<Guid>;
