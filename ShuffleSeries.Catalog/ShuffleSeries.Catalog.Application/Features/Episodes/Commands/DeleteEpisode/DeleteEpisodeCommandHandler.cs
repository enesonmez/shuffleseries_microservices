using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.DeleteEpisode;

public sealed class DeleteEpisodeCommandHandler : IRequestHandler<DeleteEpisodeCommand>
{
    private readonly IEpisodeRepository _episodeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEpisodeCommandHandler(
        IEpisodeRepository episodeRepository,
        IUnitOfWork unitOfWork)
    {
        _episodeRepository = episodeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteEpisodeCommand request, CancellationToken cancellationToken)
    {
        var episode = await _episodeRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new EpisodeNotFoundException(request.Id);

        episode.Delete();
        _episodeRepository.Delete(episode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
