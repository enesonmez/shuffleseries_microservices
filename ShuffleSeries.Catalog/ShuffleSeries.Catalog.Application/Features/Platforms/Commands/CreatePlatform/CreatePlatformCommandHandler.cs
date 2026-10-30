using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Platforms.Commands.CreatePlatform;

public sealed class CreatePlatformCommandHandler : IRequestHandler<CreatePlatformCommand, Guid>
{
    private readonly IPlatformRepository _platformRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePlatformCommandHandler(
        IPlatformRepository platformRepository,
        ICatalogCacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _platformRepository = platformRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreatePlatformCommand request, CancellationToken cancellationToken)
    {
        if (await _platformRepository.ExistsBySlugAsync(request.Slug, cancellationToken))
        {
            throw new PlatformSlugAlreadyExistsException(request.Slug);
        }

        var platform = Platform.Create(
            request.Name,
            request.Slug,
            request.LogoUrl,
            request.InactiveLogoUrl,
            request.BaseWebUrl,
            request.BaseAppSchemeUrl,
            request.DisplayOrder,
            request.IsActive);

        _platformRepository.Add(platform);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync(CatalogCacheKeys.Platforms, cancellationToken);

        return platform.Id;
    }
}
