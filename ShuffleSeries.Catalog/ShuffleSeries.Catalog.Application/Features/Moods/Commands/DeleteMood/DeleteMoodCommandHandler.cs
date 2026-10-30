using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.DeleteMood;

public sealed class DeleteMoodCommandHandler : IRequestHandler<DeleteMoodCommand>
{
    private readonly IMoodRepository _moodRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMoodCommandHandler(
        IMoodRepository moodRepository,
        ICatalogCacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _moodRepository = moodRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteMoodCommand request, CancellationToken cancellationToken)
    {
        var mood = await _moodRepository.GetByIdWithTranslationsAsync(request.Id, cancellationToken)
            ?? throw new MoodNotFoundException(request.Id);

        mood.Delete();
        _moodRepository.Delete(mood);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync(CatalogCacheKeys.Moods, cancellationToken);
    }
}
