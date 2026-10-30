using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Platforms.AddEpisodePlatform;

public sealed record AddEpisodePlatformCommand(
    Guid EpisodeId,
    Guid PlatformId,
    string WebUrl,
    string? AppSchemeUrl = null) : IRequest;
