using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Platforms.Commands.CreatePlatform;

public sealed record CreatePlatformCommand(
    string Name,
    string Slug,
    string LogoUrl,
    string InactiveLogoUrl,
    string BaseWebUrl,
    string? BaseAppSchemeUrl = null,
    int DisplayOrder = 0,
    bool IsActive = true) : IRequest<Guid>;
