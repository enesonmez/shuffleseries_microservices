using MediatR;
using ShuffleSeries.Catalog.Application.Common.Extensions;
using ShuffleSeries.Catalog.Application.Features.Episodes.Responses;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Queries.GetEpisodeById;

public sealed class GetEpisodeByIdQueryHandler : IRequestHandler<GetEpisodeByIdQuery, EpisodeDetailResponse>
{
    private readonly IEpisodeRepository _episodeRepository;
    private readonly ILanguageContext _languageContext;

    public GetEpisodeByIdQueryHandler(
        IEpisodeRepository episodeRepository,
        ILanguageContext languageContext)
    {
        _episodeRepository = episodeRepository;
        _languageContext = languageContext;
    }

    public async Task<EpisodeDetailResponse> Handle(GetEpisodeByIdQuery request, CancellationToken cancellationToken)
    {
        var episode = await _episodeRepository.GetByIdForDetailAsync(request.Id, cancellationToken)
            ?? throw new EpisodeNotFoundException(request.Id);

        var targetLanguage = request.Language ?? _languageContext.CurrentLanguage;
        var localized = episode.GetLocalizedTranslation(targetLanguage);

        var title = localized?.Title ?? string.Empty;
        var overview = localized?.Overview;
        var resolvedLanguage = localized?.LanguageCode.Value ?? targetLanguage;

        var platforms = episode.EpisodePlatforms
            .Where(ep => ep.Platform != null)
            .Select(ep => new EpisodePlatformDto(
                ep.PlatformId,
                ep.Platform!.Name,
                ep.Platform!.Slug,
                ep.Platform!.LogoUrl,
                ep.WebUrl,
                ep.AppSchemeUrl))
            .ToList();

        var translations = episode.Translations
            .Select(t => new EpisodeTranslationDto(
                t.LanguageCode.Value,
                t.Title,
                t.Overview))
            .ToList();

        return new EpisodeDetailResponse(
            episode.Id,
            episode.SeasonId,
            episode.EpisodeNumber,
            episode.IsStandalone,
            episode.RuntimeMinutes,
            episode.ImdbRating,
            episode.ThumbnailUrl,
            title,
            overview,
            resolvedLanguage,
            platforms,
            translations);
    }
}
