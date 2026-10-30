using MediatR;
using ShuffleSeries.Catalog.Application.Common.Extensions;
using ShuffleSeries.Catalog.Application.Features.Series.Responses;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesById;

public sealed class GetSeriesByIdQueryHandler : IRequestHandler<GetSeriesByIdQuery, SeriesDetailResponse>
{
    private readonly ISeriesRepository _seriesRepository;
    private readonly ILanguageContext _languageContext;

    public GetSeriesByIdQueryHandler(
        ISeriesRepository seriesRepository,
        ILanguageContext languageContext)
    {
        _seriesRepository = seriesRepository;
        _languageContext = languageContext;
    }

    public async Task<SeriesDetailResponse> Handle(GetSeriesByIdQuery request, CancellationToken cancellationToken)
    {
        var series = await _seriesRepository.GetByIdForDetailAsync(request.Id, cancellationToken)
            ?? throw new SeriesNotFoundException(request.Id);

        var targetLanguage = request.Language ?? _languageContext.CurrentLanguage;
        var localized = series.GetLocalizedTranslation(targetLanguage);

        var title = localized?.Title ?? string.Empty;
        var overview = localized?.Overview ?? string.Empty;
        var tagline = localized?.Tagline;
        var resolvedLanguage = localized?.LanguageCode.Value ?? targetLanguage;

        var translations = series.Translations
            .Select(t => new SeriesTranslationDto(t.LanguageCode.Value, t.Title, t.Overview, t.Tagline))
            .ToList();

        var seasons = series.Seasons
            .OrderBy(s => s.SeasonNumber)
            .Select(s =>
            {
                var episodes = s.Episodes
                    .OrderBy(e => e.EpisodeNumber)
                    .Select(e =>
                    {
                        var epLocalized = e.GetLocalizedTranslation(targetLanguage);
                        return new SeriesEpisodeDto(
                            e.Id,
                            e.EpisodeNumber,
                            e.IsStandalone,
                            e.RuntimeMinutes,
                            e.ImdbRating,
                            e.ThumbnailUrl,
                            epLocalized?.Title ?? string.Empty,
                            epLocalized?.Overview);
                    })
                    .ToList();

                return new SeriesSeasonDto(
                    s.Id,
                    s.SeasonNumber,
                    s.Title,
                    episodes);
            })
            .ToList();

        var platforms = series.MediaPlatforms
            .Where(mp => mp.Platform != null)
            .Select(mp => new SeriesPlatformDto(
                mp.PlatformId,
                mp.Platform!.Name,
                mp.Platform!.Slug,
                mp.Platform!.LogoUrl,
                mp.WebUrl,
                mp.AppSchemeUrl))
            .ToList();

        var moods = series.MediaMoods
            .Where(mm => mm.Mood != null)
            .Select(mm =>
            {
                var mood = mm.Mood!;
                var moodTranslation = mood.GetLocalizedTranslation(targetLanguage);
                return new SeriesMoodDto(
                    mm.MoodId,
                    mood.Code,
                    moodTranslation?.Name ?? mood.Code,
                    mood.AmbientColorHex,
                    mood.IconUrl);
            })
            .ToList();

        var genres = series.MediaGenres
            .Where(mg => mg.Genre != null)
            .Select(mg =>
            {
                var genre = mg.Genre!;
                var genreTranslation = genre.GetLocalizedTranslation(targetLanguage);
                return new SeriesGenreDto(
                    mg.GenreId,
                    genre.Code,
                    genreTranslation?.Name ?? genre.Code);
            })
            .ToList();

        return new SeriesDetailResponse(
            series.Id,
            series.TmdbId,
            series.ImdbId,
            series.ReleaseYear,
            series.SeriesType,
            series.TotalSeasons,
            series.PosterUrl,
            series.BackdropUrl,
            series.ImdbRating,
            series.ImdbVotes,
            translations,
            title,
            overview,
            tagline,
            resolvedLanguage,
            seasons,
            platforms,
            moods,
            genres);
    }
}
