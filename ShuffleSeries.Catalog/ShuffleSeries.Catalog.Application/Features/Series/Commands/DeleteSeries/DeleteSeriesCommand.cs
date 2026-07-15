using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.DeleteSeries;

public record DeleteSeriesCommand(Guid Id): IRequest;