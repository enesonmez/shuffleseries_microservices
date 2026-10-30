using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.Translations.AddOrUpdateMoodTranslation;

public sealed class AddOrUpdateMoodTranslationCommandHandler : IRequestHandler<AddOrUpdateMoodTranslationCommand>
{
    private readonly IMoodRepository _moodRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public AddOrUpdateMoodTranslationCommandHandler(
        IMoodRepository moodRepository,
        ICatalogCacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _moodRepository = moodRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddOrUpdateMoodTranslationCommand request, CancellationToken cancellationToken)
    {
        var mood = await _moodRepository.GetByIdWithTranslationsAsync(request.MoodId, cancellationToken)
            ?? throw new MoodNotFoundException(request.MoodId);

        mood.AddOrUpdateTranslation(request.LanguageCode, request.Name, request.Description, request.VibeTags);

        _moodRepository.Update(mood);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CatalogCacheKeys.Moods);
    }
}
