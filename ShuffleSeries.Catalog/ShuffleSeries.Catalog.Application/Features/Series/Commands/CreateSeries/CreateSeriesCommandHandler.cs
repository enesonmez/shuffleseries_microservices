using ShuffleSeries.Catalog.Application.Common.Services;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;

public sealed class CreateSeriesCommandHandler
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

        await MediaTaxonomyApplier.ApplyAsync(
            series,
            request.PlatformIds,
            request.MoodIds,
            request.GenreIds,
            _platformRepository,
            _moodRepository,
            _genreRepository,
            cancellationToken);

        _seriesRepository.Add(series);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return series.Id;
    }
}
