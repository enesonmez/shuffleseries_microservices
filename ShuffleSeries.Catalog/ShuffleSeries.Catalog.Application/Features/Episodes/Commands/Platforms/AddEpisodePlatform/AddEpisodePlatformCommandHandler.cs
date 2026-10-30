using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Platforms.AddEpisodePlatform;

public sealed class AddEpisodePlatformCommandHandler : IRequestHandler<AddEpisodePlatformCommand>
{
    private readonly IEpisodeRepository _episodeRepository;
    private readonly IPlatformRepository _platformRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddEpisodePlatformCommandHandler(
        IEpisodeRepository episodeRepository,
        IPlatformRepository platformRepository,
        IUnitOfWork unitOfWork)
    {
        _episodeRepository = episodeRepository;
        _platformRepository = platformRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddEpisodePlatformCommand request, CancellationToken cancellationToken)
    {
        var episode = await _episodeRepository.GetByIdWithDetailsAsync(request.EpisodeId, cancellationToken)
            ?? throw new EpisodeNotFoundException(request.EpisodeId);

        var platform = await _platformRepository.GetByIdAsync(request.PlatformId, cancellationToken)
            ?? throw new PlatformNotFoundException(request.PlatformId);

        episode.AddPlatform(platform.Id, request.WebUrl, request.AppSchemeUrl);

        _episodeRepository.Update(episode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
