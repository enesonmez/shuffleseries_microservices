using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.CreateMood;

public sealed record CreateMoodCommand(
    string Code,
    string AmbientColorHex,
    string Name,
    string Description,
    string LanguageCode,
    string? VibeTags = null,
    string? IconUrl = null,
    int DisplayOrder = 0,
    bool IsActive = true) : IRequest<Guid>;
