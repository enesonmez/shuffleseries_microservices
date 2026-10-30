using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Platforms.Commands.UpdatePlatform;

public sealed class UpdatePlatformCommandHandler : IRequestHandler<UpdatePlatformCommand>
{
    private readonly IPlatformRepository _platformRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePlatformCommandHandler(
        IPlatformRepository platformRepository,
        ICatalogCacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _platformRepository = platformRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdatePlatformCommand request, CancellationToken cancellationToken)
    {
        var platform = await _platformRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new PlatformNotFoundException(request.Id);

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (!string.Equals(platform.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase) &&
            await _platformRepository.ExistsBySlugAsync(normalizedSlug, cancellationToken))
        {
            throw new PlatformSlugAlreadyExistsException(normalizedSlug);
        }

        platform.Update(
            request.Name,
            normalizedSlug,
            request.LogoUrl,
            request.InactiveLogoUrl,
            request.BaseWebUrl,
            request.BaseAppSchemeUrl,
            request.DisplayOrder);

        _platformRepository.Update(platform);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync(CatalogCacheKeys.Platforms, cancellationToken);
    }
}
