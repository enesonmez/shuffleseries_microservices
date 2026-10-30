using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Translations.AddOrUpdateEpisodeTranslation;

public sealed class AddOrUpdateEpisodeTranslationCommandHandler : IRequestHandler<AddOrUpdateEpisodeTranslationCommand>
{
    private readonly IEpisodeRepository _episodeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddOrUpdateEpisodeTranslationCommandHandler(
        IEpisodeRepository episodeRepository,
        IUnitOfWork unitOfWork)
    {
        _episodeRepository = episodeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddOrUpdateEpisodeTranslationCommand request, CancellationToken cancellationToken)
    {
        var episode = await _episodeRepository.GetByIdWithDetailsAsync(request.EpisodeId, cancellationToken)
            ?? throw new EpisodeNotFoundException(request.EpisodeId);

        episode.AddOrUpdateTranslation(request.LanguageCode, request.Title, request.Overview);

        _episodeRepository.Update(episode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
