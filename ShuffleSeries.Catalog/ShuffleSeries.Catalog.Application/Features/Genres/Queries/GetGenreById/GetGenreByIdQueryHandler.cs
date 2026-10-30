using MediatR;
using ShuffleSeries.Catalog.Application.Common.Extensions;
using ShuffleSeries.Catalog.Application.Features.Genres.Responses;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Queries.GetGenreById;

public sealed class GetGenreByIdQueryHandler : IRequestHandler<GetGenreByIdQuery, GenreResponse>
{
    private readonly IGenreRepository _genreRepository;
    private readonly ILanguageContext _languageContext;

    public GetGenreByIdQueryHandler(
        IGenreRepository genreRepository,
        ILanguageContext languageContext)
    {
        _genreRepository = genreRepository;
        _languageContext = languageContext;
    }

    public async Task<GenreResponse> Handle(GetGenreByIdQuery request, CancellationToken cancellationToken)
    {
        var genre = await _genreRepository.GetByIdForDetailAsync(request.Id, cancellationToken)
            ?? throw new GenreNotFoundException(request.Id);

        var targetLanguage = request.Language ?? _languageContext.CurrentLanguage;
        var localized = genre.GetLocalizedTranslation(targetLanguage);

        var translations = genre.Translations
            .Select(t => new GenreTranslationResponse(t.LanguageCode.Value, t.Name, t.Description))
            .ToList();

        return new GenreResponse(
            genre.Id,
            genre.Code,
            genre.DisplayOrder,
            genre.IsActive,
            localized?.Name ?? genre.Code,
            localized?.Description,
            translations);
    }
}
