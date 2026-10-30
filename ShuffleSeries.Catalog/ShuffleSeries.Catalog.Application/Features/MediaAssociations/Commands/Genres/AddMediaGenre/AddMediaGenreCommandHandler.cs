using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Genres.AddMediaGenre;

public sealed class AddMediaGenreCommandHandler : IRequestHandler<AddMediaGenreCommand>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IGenreRepository _genreRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddMediaGenreCommandHandler(
        IMediaRepository mediaRepository,
        IGenreRepository genreRepository,
        IUnitOfWork unitOfWork)
    {
        _mediaRepository = mediaRepository;
        _genreRepository = genreRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddMediaGenreCommand request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdWithGenresAsync(request.MediaId, cancellationToken)
            ?? throw new MediaNotFoundException(request.MediaId);

        _ = await _genreRepository.GetByIdAsync(request.GenreId, cancellationToken)
            ?? throw new GenreNotFoundException(request.GenreId);

        media.AddGenre(request.GenreId);

        _mediaRepository.Update(media);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
