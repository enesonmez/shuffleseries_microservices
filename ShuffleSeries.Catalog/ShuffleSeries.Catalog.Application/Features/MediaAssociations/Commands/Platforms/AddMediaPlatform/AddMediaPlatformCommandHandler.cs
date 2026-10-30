using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Platforms.AddMediaPlatform;

public sealed class AddMediaPlatformCommandHandler : IRequestHandler<AddMediaPlatformCommand>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IPlatformRepository _platformRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddMediaPlatformCommandHandler(
        IMediaRepository mediaRepository,
        IPlatformRepository platformRepository,
        IUnitOfWork unitOfWork)
    {
        _mediaRepository = mediaRepository;
        _platformRepository = platformRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddMediaPlatformCommand request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdWithPlatformsAsync(request.MediaId, cancellationToken)
            ?? throw new MediaNotFoundException(request.MediaId);

        var platform = await _platformRepository.GetByIdAsync(request.PlatformId, cancellationToken)
            ?? throw new PlatformNotFoundException(request.PlatformId);

        media.AddPlatform(platform.Id, request.WebUrl, request.AppSchemeUrl);

        _mediaRepository.Update(media);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
