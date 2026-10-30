using MediatR;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Commands.CreateGenre;

public sealed class CreateGenreCommandHandler : IRequestHandler<CreateGenreCommand, Guid>
{
    private readonly IGenreRepository _genreRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGenreCommandHandler(
        IGenreRepository genreRepository,
        ICatalogCacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _genreRepository = genreRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateGenreCommand request, CancellationToken cancellationToken)
    {
        if (await _genreRepository.ExistsByCodeAsync(request.Code, cancellationToken))
        {
            throw new GenreCodeAlreadyExistsException(request.Code);
        }

        var genre = Genre.Create(
            request.Code,
            request.DisplayOrder,
            request.IsActive);

        genre.AddOrUpdateTranslation(
            request.LanguageCode,
            request.Name,
            request.Description);

        _genreRepository.Add(genre);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CatalogCacheKeys.Genres);

        return genre.Id;
    }
}
