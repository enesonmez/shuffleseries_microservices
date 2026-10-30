using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Commands.Translations.AddOrUpdateGenreTranslation;

public sealed record AddOrUpdateGenreTranslationCommand(
    Guid GenreId,
    string LanguageCode,
    string Name,
    string? Description = null) : IRequest;
