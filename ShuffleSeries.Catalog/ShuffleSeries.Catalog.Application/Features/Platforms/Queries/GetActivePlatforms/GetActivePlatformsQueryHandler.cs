using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Platforms.Responses;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Platforms.Queries.GetActivePlatforms;

public sealed class GetActivePlatformsQueryHandler : IRequestHandler<GetActivePlatformsQuery, IReadOnlyList<PlatformResponse>>
{
    private readonly IPlatformRepository _platformRepository;
    private readonly ICatalogCacheService _cacheService;

    public GetActivePlatformsQueryHandler(
        IPlatformRepository platformRepository,
        ICatalogCacheService cacheService)
    {
        _platformRepository = platformRepository;
        _cacheService = cacheService;
    }

    public async Task<IReadOnlyList<PlatformResponse>> Handle(GetActivePlatformsQuery request, CancellationToken cancellationToken)
    {
        return await _cacheService.GetOrCreateAsync(
            CatalogCacheKeys.Platforms,
            async () =>
            {
                var platforms = await _platformRepository.GetActivePlatformsAsync(cancellationToken);

                return platforms
                    .OrderBy(p => p.DisplayOrder)
                    .Select(p => new PlatformResponse(
                        p.Id,
                        p.Name,
                        p.Slug,
                        p.LogoUrl,
                        p.InactiveLogoUrl,
                        p.BaseWebUrl,
                        p.BaseAppSchemeUrl,
                        p.DisplayOrder,
                        p.IsActive))
                    .ToList();
            },
            CatalogCacheKeys.DefaultMetadataExpiration,
            cancellationToken);
    }
}
