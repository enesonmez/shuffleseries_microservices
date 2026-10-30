using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Commands.DeleteMood;

public sealed record DeleteMoodCommand(Guid Id) : IRequest;
