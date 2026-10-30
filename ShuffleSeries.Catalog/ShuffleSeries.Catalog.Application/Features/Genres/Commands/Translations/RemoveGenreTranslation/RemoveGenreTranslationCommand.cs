using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Commands.Translations.RemoveGenreTranslation;

public sealed record RemoveGenreTranslationCommand(
    Guid GenreId,
    string LanguageCode) : IRequest;
