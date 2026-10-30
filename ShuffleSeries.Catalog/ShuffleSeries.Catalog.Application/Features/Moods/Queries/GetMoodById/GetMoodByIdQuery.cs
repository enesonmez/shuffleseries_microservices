using MediatR;
using ShuffleSeries.Catalog.Application.Features.Moods.Responses;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Queries.GetMoodById;

public sealed record GetMoodByIdQuery(Guid Id, string? Language = null) : IRequest<MoodResponse>;
