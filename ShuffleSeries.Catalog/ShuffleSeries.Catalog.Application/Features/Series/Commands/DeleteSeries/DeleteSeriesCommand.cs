using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.DeleteSeries;

public sealed record DeleteSeriesCommand(Guid Id) : IRequest;
