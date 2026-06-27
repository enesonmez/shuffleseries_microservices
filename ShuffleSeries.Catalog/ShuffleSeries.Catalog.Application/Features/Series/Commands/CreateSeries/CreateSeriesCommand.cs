using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;

public record CreateSeriesCommand(string Title, string Description, bool IsIndependentEpisodes) : IRequest<Guid>;