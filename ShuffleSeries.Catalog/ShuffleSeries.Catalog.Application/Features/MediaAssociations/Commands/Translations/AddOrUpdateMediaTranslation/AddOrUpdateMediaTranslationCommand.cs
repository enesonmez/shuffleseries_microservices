using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Translations.AddOrUpdateMediaTranslation;

public sealed record AddOrUpdateMediaTranslationCommand(
    Guid MediaId,
    string LanguageCode,
    string Title,
    string Overview,
    string? Tagline = null) : IRequest;
