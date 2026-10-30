using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Commands.Translations.RemoveGenreTranslation;

public sealed class RemoveGenreTranslationCommandHandler : IRequestHandler<RemoveGenreTranslationCommand>
{
    private readonly IGenreRepository _genreRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveGenreTranslationCommandHandler(
        IGenreRepository genreRepository,
        ICatalogCacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _genreRepository = genreRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveGenreTranslationCommand request, CancellationToken cancellationToken)
    {
        var genre = await _genreRepository.GetByIdWithTranslationsAsync(request.GenreId, cancellationToken)
            ?? throw new GenreNotFoundException(request.GenreId);

        genre.RemoveTranslation(request.LanguageCode);

        _genreRepository.Update(genre);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync(CatalogCacheKeys.Genres, cancellationToken);
    }
}
