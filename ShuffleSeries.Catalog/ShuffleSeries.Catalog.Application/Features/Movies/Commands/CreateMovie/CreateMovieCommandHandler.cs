using MediatR;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Movies.Commands.CreateMovie;

public sealed class CreateMovieCommandHandler : IRequestHandler<CreateMovieCommand, Guid>
{
    private readonly IMovieRepository _movieRepository;
    private readonly IPlatformRepository _platformRepository;
    private readonly IMoodRepository _moodRepository;
    private readonly IGenreRepository _genreRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMovieCommandHandler(
        IMovieRepository movieRepository,
        IPlatformRepository platformRepository,
        IMoodRepository moodRepository,
        IGenreRepository genreRepository,
        IUnitOfWork unitOfWork)
    {
        _movieRepository = movieRepository;
        _platformRepository = platformRepository;
        _moodRepository = moodRepository;
        _genreRepository = genreRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateMovieCommand request, CancellationToken cancellationToken)
    {
        if (await _movieRepository.ExistsByTmdbIdAsync(request.TmdbId, null, cancellationToken))
        {
            throw new DuplicateTmdbIdException(request.TmdbId);
        }

        if (await _movieRepository.ExistsByImdbIdAsync(request.ImdbId, null, cancellationToken))
        {
            throw new DuplicateImdbIdException(request.ImdbId);
        }

        var movie = Movie.Create(
            request.TmdbId,
            request.ImdbId,
            request.ReleaseYear,
            request.RuntimeMinutes,
            request.PosterUrl,
            request.BackdropUrl,
            request.ImdbRating,
            request.ImdbVotes,
            request.Director);

        movie.AddOrUpdateTranslation(
            request.LanguageCode,
            request.Title,
            request.Overview,
            request.Tagline);

        // 1. Validate & Add Platforms (In-Memory Batched Lookup & FK Defense)
        if (request.PlatformIds is not null && request.PlatformIds.Count > 0)
        {
            var distinctPlatformIds = request.PlatformIds.Distinct().ToList();
            var activePlatforms = (await _platformRepository.GetActivePlatformsAsync(cancellationToken))
                .ToDictionary(p => p.Id);

            foreach (var platformId in distinctPlatformIds)
            {
                if (!activePlatforms.TryGetValue(platformId, out var platform))
                {
                    throw new PlatformNotFoundException(platformId);
                }

                movie.AddPlatform(platform.Id, platform.BaseWebUrl);
            }
        }

        // 2. Validate & Add Moods (In-Memory Batched Lookup & FK Defense)
        if (request.MoodIds is not null && request.MoodIds.Count > 0)
        {
            var distinctMoodIds = request.MoodIds.Distinct().ToList();
            var activeMoods = (await _moodRepository.GetActiveMoodsWithTranslationsAsync(cancellationToken))
                .ToDictionary(m => m.Id);

            foreach (var moodId in distinctMoodIds)
            {
                if (!activeMoods.TryGetValue(moodId, out var mood))
                {
                    throw new MoodNotFoundException(moodId);
                }

                movie.AddMood(mood.Id);
            }
        }

        // 3. Validate & Add Genres (In-Memory Batched Lookup & FK Defense)
        if (request.GenreIds is not null && request.GenreIds.Count > 0)
        {
            var distinctGenreIds = request.GenreIds.Distinct().ToList();
            var activeGenres = (await _genreRepository.GetActiveGenresWithTranslationsAsync(cancellationToken))
                .ToDictionary(g => g.Id);

            foreach (var genreId in distinctGenreIds)
            {
                if (!activeGenres.TryGetValue(genreId, out var genre))
                {
                    throw new GenreNotFoundException(genreId);
                }

                movie.AddGenre(genre.Id);
            }
        }

        _movieRepository.Add(movie);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return movie.Id;
    }
}
