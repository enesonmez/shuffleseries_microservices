using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.CreateEpisode;

public sealed class CreateEpisodeCommandHandler : IRequestHandler<CreateEpisodeCommand, Guid>
{
    private readonly ISeriesRepository _seriesRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEpisodeCommandHandler(
        ISeriesRepository seriesRepository,
        IUnitOfWork unitOfWork)
    {
        _seriesRepository = seriesRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateEpisodeCommand request, CancellationToken cancellationToken)
    {
        var series = await _seriesRepository.GetByIdWithSeasonsAsync(request.SeriesId, cancellationToken)
            ?? throw new SeriesNotFoundException(request.SeriesId);

        var episode = series.AddEpisode(
            request.SeasonNumber,
            request.EpisodeNumber,
            request.IsStandalone,
            request.RuntimeMinutes,
            request.ImdbRating,
            request.ThumbnailUrl);

        episode.AddOrUpdateTranslation(
            request.LanguageCode,
            request.Title,
            request.Overview);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return episode.Id;
    }
}
