using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.DeleteEpisode;

public sealed record DeleteEpisodeCommand(Guid Id) : IRequest;
