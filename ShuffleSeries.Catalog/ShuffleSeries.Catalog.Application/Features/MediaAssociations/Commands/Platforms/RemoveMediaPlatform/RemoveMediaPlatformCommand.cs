using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Platforms.RemoveMediaPlatform;

public sealed record RemoveMediaPlatformCommand(Guid MediaId, Guid PlatformId) : IRequest;
