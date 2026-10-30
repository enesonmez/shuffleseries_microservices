using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Moods.RemoveMediaMood;

public sealed record RemoveMediaMoodCommand(Guid MediaId, Guid MoodId) : IRequest;
