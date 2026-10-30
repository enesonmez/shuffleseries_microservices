using MediatR;
using ShuffleSeries.Catalog.Application.Features.Platforms.Responses;

namespace ShuffleSeries.Catalog.Application.Features.Platforms.Queries.GetPlatformById;

public sealed record GetPlatformByIdQuery(Guid Id) : IRequest<PlatformResponse>;
