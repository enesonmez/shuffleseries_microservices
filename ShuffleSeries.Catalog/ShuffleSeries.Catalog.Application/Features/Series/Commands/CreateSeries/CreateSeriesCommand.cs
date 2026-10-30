using MediatR;
using ShuffleSeries.Catalog.Domain.Enums;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;

public sealed record CreateSeriesCommand(
    int TmdbId,
    string ImdbId,
    int ReleaseYear,
    SeriesType SeriesType,
    string PosterUrl,
    string? BackdropUrl,
    string LanguageCode,
    string Title,
    string Overview,
    string? Tagline = null,
    double? ImdbRating = null,
    int? ImdbVotes = null,
    int TotalSeasons = 0,
    IReadOnlyList<Guid>? PlatformIds = null,
    IReadOnlyList<Guid>? MoodIds = null,
    IReadOnlyList<Guid>? GenreIds = null) : IRequest<Guid>;
