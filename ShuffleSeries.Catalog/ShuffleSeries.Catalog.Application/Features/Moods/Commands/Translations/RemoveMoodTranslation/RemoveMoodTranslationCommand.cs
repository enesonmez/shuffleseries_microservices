using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.Translations.RemoveMoodTranslation;

public sealed record RemoveMoodTranslationCommand(
    Guid MoodId,
    string LanguageCode) : IRequest;
