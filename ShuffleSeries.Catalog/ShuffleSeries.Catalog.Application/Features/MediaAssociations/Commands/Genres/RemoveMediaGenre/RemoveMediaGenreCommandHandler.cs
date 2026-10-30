using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Genres.RemoveMediaGenre;

public sealed class RemoveMediaGenreCommandHandler : IRequestHandler<RemoveMediaGenreCommand>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveMediaGenreCommandHandler(
        IMediaRepository mediaRepository,
        IUnitOfWork unitOfWork)
    {
        _mediaRepository = mediaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveMediaGenreCommand request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdWithGenresAsync(request.MediaId, cancellationToken)
            ?? throw new MediaNotFoundException(request.MediaId);

        media.RemoveGenre(request.GenreId);

        _mediaRepository.Update(media);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
