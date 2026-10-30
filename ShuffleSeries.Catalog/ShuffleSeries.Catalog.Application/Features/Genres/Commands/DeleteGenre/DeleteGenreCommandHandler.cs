using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Commands.DeleteGenre;

public sealed class DeleteGenreCommandHandler : IRequestHandler<DeleteGenreCommand>
{
    private readonly IGenreRepository _genreRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteGenreCommandHandler(
        IGenreRepository genreRepository,
        ICatalogCacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _genreRepository = genreRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteGenreCommand request, CancellationToken cancellationToken)
    {
        var genre = await _genreRepository.GetByIdWithTranslationsAsync(request.Id, cancellationToken)
            ?? throw new GenreNotFoundException(request.Id);

        genre.Delete();
        _genreRepository.Delete(genre);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CatalogCacheKeys.Genres);
    }
}
