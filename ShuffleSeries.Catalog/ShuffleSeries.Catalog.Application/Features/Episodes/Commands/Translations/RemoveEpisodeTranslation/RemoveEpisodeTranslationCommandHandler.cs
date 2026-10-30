using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Translations.RemoveEpisodeTranslation;

public sealed class RemoveEpisodeTranslationCommandHandler : IRequestHandler<RemoveEpisodeTranslationCommand>
{
    private readonly IEpisodeRepository _episodeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveEpisodeTranslationCommandHandler(
        IEpisodeRepository episodeRepository,
        IUnitOfWork unitOfWork)
    {
        _episodeRepository = episodeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveEpisodeTranslationCommand request, CancellationToken cancellationToken)
    {
        var episode = await _episodeRepository.GetByIdWithDetailsAsync(request.EpisodeId, cancellationToken)
            ?? throw new EpisodeNotFoundException(request.EpisodeId);

        episode.RemoveTranslation(request.LanguageCode);

        _episodeRepository.Update(episode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
