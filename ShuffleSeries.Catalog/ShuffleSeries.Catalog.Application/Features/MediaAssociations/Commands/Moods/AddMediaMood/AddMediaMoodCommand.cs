using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Moods.AddMediaMood;

public sealed record AddMediaMoodCommand(Guid MediaId, Guid MoodId) : IRequest;
