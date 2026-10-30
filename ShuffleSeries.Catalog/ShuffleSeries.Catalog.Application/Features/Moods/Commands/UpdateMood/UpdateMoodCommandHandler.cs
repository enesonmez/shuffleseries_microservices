using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.UpdateMood;

public sealed class UpdateMoodCommandHandler : IRequestHandler<UpdateMoodCommand>
{
    private readonly IMoodRepository _moodRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMoodCommandHandler(
        IMoodRepository moodRepository,
        ICatalogCacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _moodRepository = moodRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateMoodCommand request, CancellationToken cancellationToken)
    {
        var mood = await _moodRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new MoodNotFoundException(request.Id);

        var normalizedCode = request.Code.Trim().ToLowerInvariant();
        if (!string.Equals(mood.Code, normalizedCode, StringComparison.OrdinalIgnoreCase) &&
            await _moodRepository.ExistsByCodeAsync(normalizedCode, cancellationToken))
        {
            throw new MoodCodeAlreadyExistsException(normalizedCode);
        }

        mood.Update(
            normalizedCode,
            request.AmbientColorHex,
            request.IconUrl,
            request.DisplayOrder);

        _moodRepository.Update(mood);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync(CatalogCacheKeys.Moods, cancellationToken);
    }
}
