using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.UpdateMood;

public sealed record UpdateMoodCommand(
    Guid Id,
    string Code,
    string AmbientColorHex,
    string? IconUrl = null,
    int DisplayOrder = 0) : IRequest;
