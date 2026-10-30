using Moq;
using ShuffleSeries.Catalog.Application.Features.Platforms.Queries.GetPlatformById;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Platforms;

public class GetPlatformByIdQueryHandlerTests
{
    private readonly Mock<IPlatformRepository> _platformRepositoryMock;
    private readonly GetPlatformByIdQueryHandler _handler;

    public GetPlatformByIdQueryHandlerTests()
    {
        _platformRepositoryMock = new Mock<IPlatformRepository>();
        _handler = new GetPlatformByIdQueryHandler(_platformRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowPlatformNotFoundException_When_PlatformDoesNotExist()
    {
        // Arrange
        var query = new GetPlatformByIdQuery(Guid.NewGuid());
        _platformRepositoryMock.Setup(x => x.GetByIdForDetailAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Platform?)null);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PlatformNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnPlatformResponse_When_PlatformExists()
    {
        // Arrange
        var platform = Platform.Create("Netflix", "netflix", "https://logo.png", "https://inactive.png", "https://netflix.com");
        var query = new GetPlatformByIdQuery(platform.Id);

        _platformRepositoryMock.Setup(x => x.GetByIdForDetailAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(platform.Id);
        result.Name.Should().Be("Netflix");
        result.Slug.Should().Be("netflix");
    }
}
