using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Translations.AddOrUpdateEpisodeTranslation;

public sealed record AddOrUpdateEpisodeTranslationCommand(
    Guid EpisodeId,
    string LanguageCode,
    string Title,
    string? Overview = null) : IRequest;
