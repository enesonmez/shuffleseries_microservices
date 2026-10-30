using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Translations.RemoveEpisodeTranslation;

public sealed record RemoveEpisodeTranslationCommand(
    Guid EpisodeId,
    string LanguageCode) : IRequest;
