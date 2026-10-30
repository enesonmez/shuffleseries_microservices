using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Platforms.RemoveEpisodePlatform;

public sealed class RemoveEpisodePlatformCommandHandler : IRequestHandler<RemoveEpisodePlatformCommand>
{
    private readonly IEpisodeRepository _episodeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveEpisodePlatformCommandHandler(
        IEpisodeRepository episodeRepository,
        IUnitOfWork unitOfWork)
    {
        _episodeRepository = episodeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveEpisodePlatformCommand request, CancellationToken cancellationToken)
    {
        var episode = await _episodeRepository.GetByIdWithDetailsAsync(request.EpisodeId, cancellationToken)
            ?? throw new EpisodeNotFoundException(request.EpisodeId);

        episode.RemovePlatform(request.PlatformId);

        _episodeRepository.Update(episode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
