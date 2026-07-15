using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;

public record UpdateSeriesCommand(Guid Id, string Title, string Description, bool IsIndependentEpisodes) : IRequest;