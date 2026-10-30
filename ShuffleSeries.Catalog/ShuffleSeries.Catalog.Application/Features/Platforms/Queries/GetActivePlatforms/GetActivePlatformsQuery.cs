using MediatR;
using ShuffleSeries.Catalog.Application.Features.Platforms.Responses;

namespace ShuffleSeries.Catalog.Application.Features.Platforms.Queries.GetActivePlatforms;

public sealed record GetActivePlatformsQuery : IRequest<IReadOnlyList<PlatformResponse>>;
