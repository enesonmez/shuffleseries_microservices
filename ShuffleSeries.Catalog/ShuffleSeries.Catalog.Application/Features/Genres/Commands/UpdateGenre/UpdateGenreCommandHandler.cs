using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Commands.UpdateGenre;

public sealed class UpdateGenreCommandHandler : IRequestHandler<UpdateGenreCommand>
{
    private readonly IGenreRepository _genreRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGenreCommandHandler(
        IGenreRepository genreRepository,
        ICatalogCacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _genreRepository = genreRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateGenreCommand request, CancellationToken cancellationToken)
    {
        var genre = await _genreRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GenreNotFoundException(request.Id);

        var normalizedCode = request.Code.Trim().ToLowerInvariant();
        if (!string.Equals(genre.Code, normalizedCode, StringComparison.OrdinalIgnoreCase) &&
            await _genreRepository.ExistsByCodeAsync(normalizedCode, cancellationToken))
        {
            throw new GenreCodeAlreadyExistsException(normalizedCode);
        }

        genre.Update(normalizedCode, request.DisplayOrder);

        _genreRepository.Update(genre);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CatalogCacheKeys.Genres);
    }
}
