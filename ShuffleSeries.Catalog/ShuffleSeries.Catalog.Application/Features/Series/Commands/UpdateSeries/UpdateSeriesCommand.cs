using MediatR;
using ShuffleSeries.Catalog.Domain.Enums;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;

public sealed record UpdateSeriesCommand(
    Guid Id,
    int ReleaseYear,
    SeriesType SeriesType,
    int TotalSeasons,
    string? ImdbId = null,
    double? ImdbRating = null,
    int? ImdbVotes = null) : IRequest;
