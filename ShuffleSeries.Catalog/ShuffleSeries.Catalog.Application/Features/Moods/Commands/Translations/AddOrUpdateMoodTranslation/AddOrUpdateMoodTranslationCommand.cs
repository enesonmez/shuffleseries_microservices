using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.Translations.AddOrUpdateMoodTranslation;

public sealed record AddOrUpdateMoodTranslationCommand(
    Guid MoodId,
    string LanguageCode,
    string Name,
    string Description,
    string? VibeTags = null) : IRequest;
