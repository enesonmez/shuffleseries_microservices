using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Platforms.RemoveMediaPlatform;

public sealed class RemoveMediaPlatformCommandHandler : IRequestHandler<RemoveMediaPlatformCommand>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveMediaPlatformCommandHandler(
        IMediaRepository mediaRepository,
        IUnitOfWork unitOfWork)
    {
        _mediaRepository = mediaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveMediaPlatformCommand request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdWithPlatformsAsync(request.MediaId, cancellationToken)
            ?? throw new MediaNotFoundException(request.MediaId);

        media.RemovePlatform(request.PlatformId);

        _mediaRepository.Update(media);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
