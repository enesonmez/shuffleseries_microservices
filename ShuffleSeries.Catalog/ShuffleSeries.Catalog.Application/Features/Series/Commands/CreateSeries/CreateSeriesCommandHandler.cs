using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;

public sealed class CreateSeriesCommandHandler : IRequestHandler<CreateSeriesCommand, Guid>
{
    private readonly ISeriesRepository _seriesRepository;
    private readonly IPlatformRepository _platformRepository;
    private readonly IMoodRepository _moodRepository;
    private readonly IGenreRepository _genreRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSeriesCommandHandler(
        ISeriesRepository seriesRepository,
        IPlatformRepository platformRepository,
        IMoodRepository moodRepository,
        IGenreRepository genreRepository,
        IUnitOfWork unitOfWork)
    {
        _seriesRepository = seriesRepository;
        _platformRepository = platformRepository;
        _moodRepository = moodRepository;
        _genreRepository = genreRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateSeriesCommand request, CancellationToken cancellationToken)
    {
        if (await _seriesRepository.ExistsByTmdbIdAsync(request.TmdbId, cancellationToken: cancellationToken))
            throw new DuplicateTmdbIdException(request.TmdbId);

        if (await _seriesRepository.ExistsByImdbIdAsync(request.ImdbId, cancellationToken: cancellationToken))
            throw new DuplicateImdbIdException(request.ImdbId);

        var series = Domain.Entities.Series.Create(
            request.TmdbId,
            request.ImdbId,
            request.ReleaseYear,
            request.SeriesType,
            request.PosterUrl,
            request.BackdropUrl,
            request.ImdbRating,
            request.ImdbVotes,
            request.TotalSeasons);

        series.AddOrUpdateTranslation(
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

                series.AddPlatform(platform.Id, platform.BaseWebUrl);
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

                series.AddMood(mood.Id);
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

                series.AddGenre(genre.Id);
            }
        }

        _seriesRepository.Add(series);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return series.Id;
    }
}
