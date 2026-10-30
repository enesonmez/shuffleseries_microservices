using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Translations.RemoveMediaTranslation;

public sealed record RemoveMediaTranslationCommand(Guid MediaId, string LanguageCode) : IRequest;
