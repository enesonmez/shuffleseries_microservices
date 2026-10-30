using MediatR;
using ShuffleSeries.Catalog.Application.Common.Extensions;
using ShuffleSeries.Catalog.Application.Features.Movies.Responses;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Movies.Queries.GetMovieById;

public sealed class GetMovieByIdQueryHandler : IRequestHandler<GetMovieByIdQuery, MovieDetailResponse>
{
    private readonly IMovieRepository _movieRepository;
    private readonly ILanguageContext _languageContext;

    public GetMovieByIdQueryHandler(
        IMovieRepository movieRepository,
        ILanguageContext languageContext)
    {
        _movieRepository = movieRepository;
        _languageContext = languageContext;
    }

    public async Task<MovieDetailResponse> Handle(GetMovieByIdQuery request, CancellationToken cancellationToken)
    {
        var movie = await _movieRepository.GetByIdForDetailAsync(request.Id, cancellationToken)
            ?? throw new MovieNotFoundException(request.Id);

        var targetLanguage = request.Language ?? _languageContext.CurrentLanguage;
        var localized = movie.GetLocalizedTranslation(targetLanguage);

        var title = localized?.Title ?? string.Empty;
        var overview = localized?.Overview ?? string.Empty;
        var tagline = localized?.Tagline;
        var resolvedLanguage = localized?.LanguageCode.Value ?? targetLanguage;

        var platforms = movie.MediaPlatforms
            .Where(mp => mp.Platform != null)
            .Select(mp => new MoviePlatformDto(
                mp.PlatformId,
                mp.Platform!.Name,
                mp.Platform!.Slug,
                mp.Platform!.LogoUrl,
                mp.WebUrl,
                mp.AppSchemeUrl))
            .ToList();

        var moods = movie.MediaMoods
            .Where(mm => mm.Mood != null)
            .Select(mm =>
            {
                var mood = mm.Mood!;
                var moodTranslation = mood.GetLocalizedTranslation(targetLanguage);
                return new MovieMoodDto(
                    mm.MoodId,
                    mood.Code,
                    moodTranslation?.Name ?? mood.Code,
                    mood.AmbientColorHex,
                    mood.IconUrl);
            })
            .ToList();

        var genres = movie.MediaGenres
            .Where(mg => mg.Genre != null)
            .Select(mg =>
            {
                var genre = mg.Genre!;
                var genreTranslation = genre.GetLocalizedTranslation(targetLanguage);
                return new MovieGenreDto(
                    mg.GenreId,
                    genre.Code,
                    genreTranslation?.Name ?? genre.Code);
            })
            .ToList();

        var translations = movie.Translations
            .Select(t => new MovieTranslationDto(
                t.LanguageCode.Value,
                t.Title,
                t.Overview,
                t.Tagline))
            .ToList();

        return new MovieDetailResponse(
            movie.Id,
            movie.TmdbId,
            movie.ImdbId,
            title,
            overview,
            tagline,
            resolvedLanguage,
            movie.ReleaseYear,
            movie.RuntimeMinutes,
            movie.Director,
            movie.PosterUrl,
            movie.BackdropUrl,
            movie.ImdbRating,
            movie.ImdbVotes,
            platforms,
            moods,
            genres,
            translations);
    }
}
