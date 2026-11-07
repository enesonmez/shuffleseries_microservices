using ShuffleSeries.Catalog.Application.Common.Services;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Movies.Commands.CreateMovie;

public sealed class CreateMovieCommandHandler
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

        await MediaTaxonomyApplier.ApplyAsync(
            movie,
            request.PlatformIds,
            request.MoodIds,
            request.GenreIds,
            _platformRepository,
            _moodRepository,
            _genreRepository,
            cancellationToken);

        _movieRepository.Add(movie);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return movie.Id;
    }
}
