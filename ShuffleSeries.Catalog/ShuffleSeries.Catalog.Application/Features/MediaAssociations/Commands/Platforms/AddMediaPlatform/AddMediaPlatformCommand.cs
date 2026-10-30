using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Platforms.AddMediaPlatform;

public sealed record AddMediaPlatformCommand(
    Guid MediaId,
    Guid PlatformId,
    string WebUrl,
    string? AppSchemeUrl = null) : IRequest;
