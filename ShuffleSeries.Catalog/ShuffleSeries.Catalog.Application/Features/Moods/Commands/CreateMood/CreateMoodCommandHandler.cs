using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.CreateMood;

public sealed class CreateMoodCommandHandler : IRequestHandler<CreateMoodCommand, Guid>
{
    private readonly IMoodRepository _moodRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMoodCommandHandler(
        IMoodRepository moodRepository,
        ICatalogCacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _moodRepository = moodRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateMoodCommand request, CancellationToken cancellationToken)
    {
        if (await _moodRepository.ExistsByCodeAsync(request.Code, cancellationToken))
        {
            throw new MoodCodeAlreadyExistsException(request.Code);
        }

        var mood = Mood.Create(
            request.Code,
            request.AmbientColorHex,
            request.IconUrl,
            request.DisplayOrder,
            request.IsActive);

        mood.AddOrUpdateTranslation(
            request.LanguageCode,
            request.Name,
            request.Description,
            request.VibeTags);

        _moodRepository.Add(mood);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync(CatalogCacheKeys.Moods, cancellationToken);

        return mood.Id;
    }
}
