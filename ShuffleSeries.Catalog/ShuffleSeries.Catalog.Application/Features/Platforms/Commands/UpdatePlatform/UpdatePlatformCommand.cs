using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Platforms.Commands.UpdatePlatform;

public sealed record UpdatePlatformCommand(
    Guid Id,
    string Name,
    string Slug,
    string LogoUrl,
    string InactiveLogoUrl,
    string BaseWebUrl,
    string? BaseAppSchemeUrl = null,
    int DisplayOrder = 0) : IRequest;
