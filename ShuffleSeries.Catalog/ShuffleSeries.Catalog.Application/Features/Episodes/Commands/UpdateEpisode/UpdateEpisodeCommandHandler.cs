using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.UpdateEpisode;

public sealed class UpdateEpisodeCommandHandler
{
    private readonly IEpisodeRepository _episodeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEpisodeCommandHandler(
        IEpisodeRepository episodeRepository,
        IUnitOfWork unitOfWork)
    {
        _episodeRepository = episodeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateEpisodeCommand request, CancellationToken cancellationToken)
    {
        var episode = await _episodeRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EpisodeNotFoundException(request.Id);

        if (episode.EpisodeNumber != request.EpisodeNumber)
        {
            var conflictingSeasonNumber = await _episodeRepository.GetConflictingSeasonNumberAsync(
                episode.SeasonId,
                request.EpisodeNumber,
                episode.Id,
                cancellationToken);

            if (conflictingSeasonNumber.HasValue)
            {
                throw new DuplicateEpisodeException(conflictingSeasonNumber.Value, request.EpisodeNumber);
            }
        }

        episode.UpdateDetails(
            request.EpisodeNumber,
            request.IsStandalone,
            request.RuntimeMinutes,
            request.ImdbRating,
            request.ThumbnailUrl);

        _episodeRepository.Update(episode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
