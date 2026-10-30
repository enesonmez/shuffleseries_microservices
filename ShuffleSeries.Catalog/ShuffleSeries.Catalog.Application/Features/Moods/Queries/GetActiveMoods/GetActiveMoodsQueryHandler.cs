using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Extensions;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Moods.Models;
using ShuffleSeries.Catalog.Application.Features.Moods.Responses;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Queries.GetActiveMoods;

public sealed class GetActiveMoodsQueryHandler : IRequestHandler<GetActiveMoodsQuery, IReadOnlyList<MoodResponse>>
{
    private readonly IMoodRepository _moodRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly ILanguageContext _languageContext;

    public GetActiveMoodsQueryHandler(
        IMoodRepository moodRepository,
        ICatalogCacheService cacheService,
        ILanguageContext languageContext)
    {
        _moodRepository = moodRepository;
        _cacheService = cacheService;
        _languageContext = languageContext;
    }

    public async Task<IReadOnlyList<MoodResponse>> Handle(GetActiveMoodsQuery request, CancellationToken cancellationToken)
    {
        var targetLanguage = request.Language ?? _languageContext.CurrentLanguage;

        var moods = await _cacheService.GetOrCreateAsync(
            CatalogCacheKeys.Moods,
            async () =>
            {
                var list = await _moodRepository.GetActiveMoodsWithTranslationsAsync(cancellationToken);
                return list
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => new CachedMoodItem(
                        m.Id,
                        m.Code,
                        m.AmbientColorHex,
                        m.IconUrl,
                        m.DisplayOrder,
                        m.IsActive,
                        m.Translations
                            .Select(t => new CachedMoodTranslationItem(t.LanguageCode.Value, t.Name, t.Description ?? string.Empty, t.VibeTags))
                            .ToList()))
                    .ToList();
            },
            CatalogCacheKeys.DefaultMetadataExpiration,
            cancellationToken);

        return moods
            .Select(m =>
            {
                var localized = m.GetLocalizedTranslation(targetLanguage);
                var translations = m.Translations
                    .Select(t => new MoodTranslationResponse(t.LanguageCode, t.Name, t.Description, t.VibeTags))
                    .ToList();

                return new MoodResponse(
                    m.Id,
                    m.Code,
                    m.AmbientColorHex,
                    m.IconUrl,
                    m.DisplayOrder,
                    m.IsActive,
                    localized?.Name ?? m.Code,
                    localized?.Description ?? string.Empty,
                    localized?.VibeTags,
                    translations);
            })
            .ToList();
    }
}
