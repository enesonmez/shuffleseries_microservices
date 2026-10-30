using MediatR;
using ShuffleSeries.Catalog.Domain.Enums;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;

public sealed record UpdateSeriesCommand(
    Guid Id,
    int TmdbId,
    string ImdbId,
    int ReleaseYear,
    SeriesType SeriesType,
    int TotalSeasons,
    double? ImdbRating = null,
    int? ImdbVotes = null,
    string? PosterUrl = null,
    string? BackdropUrl = null) : IRequest;
