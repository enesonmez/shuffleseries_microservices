using MediatR;
using ShuffleSeries.Catalog.Application.Features.Moods.Responses;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Queries.GetActiveMoods;

public sealed record GetActiveMoodsQuery(string? Language = null) : IRequest<IReadOnlyList<MoodResponse>>;
