using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Translations.RemoveMediaTranslation;

public sealed class RemoveMediaTranslationCommandHandler : IRequestHandler<RemoveMediaTranslationCommand>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveMediaTranslationCommandHandler(
        IMediaRepository mediaRepository,
        IUnitOfWork unitOfWork)
    {
        _mediaRepository = mediaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveMediaTranslationCommand request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdWithTranslationsAsync(request.MediaId, cancellationToken)
            ?? throw new MediaNotFoundException(request.MediaId);

        media.RemoveTranslation(request.LanguageCode);

        _mediaRepository.Update(media);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
