using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Platforms.Queries.GetActivePlatforms;
using ShuffleSeries.Catalog.Application.Features.Platforms.Responses;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Platforms;

public class GetActivePlatformsQueryHandlerTests
{
    private readonly Mock<IPlatformRepository> _platformRepositoryMock;
    private readonly Mock<ICatalogCacheService> _cacheServiceMock;
    private readonly GetActivePlatformsQueryHandler _handler;

    public GetActivePlatformsQueryHandlerTests()
    {
        _platformRepositoryMock = new Mock<IPlatformRepository>();
        _cacheServiceMock = new Mock<ICatalogCacheService>();
        _handler = new GetActivePlatformsQueryHandler(
            _platformRepositoryMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnOrderedPlatformsFromCache()
    {
        // Arrange
        var p1 = Platform.Create("Netflix", "netflix", "logo1", "inact1", "https://netflix.com", displayOrder: 2);
        var p2 = Platform.Create("Amazon Prime", "prime", "logo2", "inact2", "https://prime.com", displayOrder: 1);
        var platformsList = new List<Platform> { p1, p2 };

        _platformRepositoryMock.Setup(r => r.GetActivePlatformsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(platformsList);

        _cacheServiceMock.Setup(x => x.GetOrCreateAsync(
                CatalogCacheKeys.Platforms,
                It.IsAny<Func<Task<List<PlatformResponse>>>>(),
                CatalogCacheKeys.DefaultMetadataExpiration,
                It.IsAny<CancellationToken>()))
            .Returns((string key, Func<Task<List<PlatformResponse>>> factory, TimeSpan? exp, CancellationToken ct) => factory());

        // Act
        var result = await _handler.Handle(new GetActivePlatformsQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Slug.Should().Be("prime");
        result[1].Slug.Should().Be("netflix");
    }
}
