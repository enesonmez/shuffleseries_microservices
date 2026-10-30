using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Platforms.RemoveEpisodePlatform;

public sealed record RemoveEpisodePlatformCommand(
    Guid EpisodeId,
    Guid PlatformId) : IRequest;
