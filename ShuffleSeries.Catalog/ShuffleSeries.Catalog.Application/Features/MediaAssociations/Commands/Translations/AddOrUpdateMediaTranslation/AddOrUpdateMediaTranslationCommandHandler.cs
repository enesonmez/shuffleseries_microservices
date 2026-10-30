using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Translations.AddOrUpdateMediaTranslation;

public sealed class AddOrUpdateMediaTranslationCommandHandler : IRequestHandler<AddOrUpdateMediaTranslationCommand>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddOrUpdateMediaTranslationCommandHandler(
        IMediaRepository mediaRepository,
        IUnitOfWork unitOfWork)
    {
        _mediaRepository = mediaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddOrUpdateMediaTranslationCommand request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdWithTranslationsAsync(request.MediaId, cancellationToken)
            ?? throw new MediaNotFoundException(request.MediaId);

        media.AddOrUpdateTranslation(
            request.LanguageCode,
            request.Title,
            request.Overview,
            request.Tagline);

        _mediaRepository.Update(media);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
