using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Platforms.Commands.DeletePlatform;

public sealed record DeletePlatformCommand(Guid Id) : IRequest;
