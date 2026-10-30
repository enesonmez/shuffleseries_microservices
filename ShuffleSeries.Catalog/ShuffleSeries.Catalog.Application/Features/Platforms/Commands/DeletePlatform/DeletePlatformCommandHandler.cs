using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Platforms.Commands.DeletePlatform;

public sealed class DeletePlatformCommandHandler : IRequestHandler<DeletePlatformCommand>
{
    private readonly IPlatformRepository _platformRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePlatformCommandHandler(
        IPlatformRepository platformRepository,
        ICatalogCacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _platformRepository = platformRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeletePlatformCommand request, CancellationToken cancellationToken)
    {
        var platform = await _platformRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new PlatformNotFoundException(request.Id);

        platform.Delete();
        _platformRepository.Delete(platform);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync(CatalogCacheKeys.Platforms, cancellationToken);
    }
}
