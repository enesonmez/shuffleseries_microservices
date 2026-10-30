using MediatR;
using ShuffleSeries.Catalog.Application.Features.Platforms.Responses;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Platforms.Queries.GetPlatformById;

public sealed class GetPlatformByIdQueryHandler : IRequestHandler<GetPlatformByIdQuery, PlatformResponse>
{
    private readonly IPlatformRepository _platformRepository;

    public GetPlatformByIdQueryHandler(IPlatformRepository platformRepository)
    {
        _platformRepository = platformRepository;
    }

    public async Task<PlatformResponse> Handle(GetPlatformByIdQuery request, CancellationToken cancellationToken)
    {
        var platform = await _platformRepository.GetByIdForDetailAsync(request.Id, cancellationToken)
            ?? throw new PlatformNotFoundException(request.Id);

        return new PlatformResponse(
            platform.Id,
            platform.Name,
            platform.Slug,
            platform.LogoUrl,
            platform.InactiveLogoUrl,
            platform.BaseWebUrl,
            platform.BaseAppSchemeUrl,
            platform.DisplayOrder,
            platform.IsActive);
    }
}
