using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Extensions;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Genres.Models;
using ShuffleSeries.Catalog.Application.Features.Genres.Responses;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Queries.GetActiveGenres;

public sealed class GetActiveGenresQueryHandler : IRequestHandler<GetActiveGenresQuery, IReadOnlyList<GenreResponse>>
{
    private readonly IGenreRepository _genreRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly ILanguageContext _languageContext;

    public GetActiveGenresQueryHandler(
        IGenreRepository genreRepository,
        ICatalogCacheService cacheService,
        ILanguageContext languageContext)
    {
        _genreRepository = genreRepository;
        _cacheService = cacheService;
        _languageContext = languageContext;
    }

    public async Task<IReadOnlyList<GenreResponse>> Handle(GetActiveGenresQuery request, CancellationToken cancellationToken)
    {
        var targetLanguage = request.Language ?? _languageContext.CurrentLanguage;

        var genres = await _cacheService.GetOrCreateAsync(
            CatalogCacheKeys.Genres,
            async () =>
            {
                var list = await _genreRepository.GetActiveGenresWithTranslationsAsync(cancellationToken);
                return list
                    .OrderBy(g => g.DisplayOrder)
                    .Select(g => new CachedGenreItem(
                        g.Id,
                        g.Code,
                        g.DisplayOrder,
                        g.IsActive,
                        g.Translations
                            .Select(t => new CachedGenreTranslationItem(t.LanguageCode.Value, t.Name, t.Description))
                            .ToList()))
                    .ToList();
            },
            CatalogCacheKeys.DefaultMetadataExpiration,
            cancellationToken);

        return genres
            .Select(g =>
            {
                var localized = g.GetLocalizedTranslation(targetLanguage);
                var translations = g.Translations
                    .Select(t => new GenreTranslationResponse(t.LanguageCode, t.Name, t.Description))
                    .ToList();

                return new GenreResponse(
                    g.Id,
                    g.Code,
                    g.DisplayOrder,
                    g.IsActive,
                    localized?.Name ?? g.Code,
                    localized?.Description,
                    translations);
            })
            .ToList();
    }
}
