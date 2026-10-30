using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Moods.Models;
using ShuffleSeries.Catalog.Application.Features.Moods.Queries.GetActiveMoods;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Moods;

public class GetActiveMoodsQueryHandlerTests
{
    private readonly Mock<IMoodRepository> _moodRepositoryMock = new();
    private readonly Mock<ICatalogCacheService> _cacheServiceMock = new();
    private readonly Mock<ILanguageContext> _languageContextMock = new();
    private readonly GetActiveMoodsQueryHandler _handler;

    public GetActiveMoodsQueryHandlerTests()
    {
        _languageContextMock.Setup(x => x.CurrentLanguage).Returns("tr");

        // Mock cache to directly invoke factory
        _cacheServiceMock.Setup(x => x.GetOrCreateAsync(
                CatalogCacheKeys.Moods,
                It.IsAny<Func<Task<List<CachedMoodItem>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<List<CachedMoodItem>>>, TimeSpan?, CancellationToken>(
                (_, factory, _, _) => factory());

        _handler = new GetActiveMoodsQueryHandler(
            _moodRepositoryMock.Object,
            _cacheServiceMock.Object,
            _languageContextMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnActiveMoodsOrderedByDisplayOrder()
    {
        var mood1 = Mood.Create("chill", "#4A90E2", null, 2);
        mood1.AddOrUpdateTranslation("tr", "Sakin", "Dinlendirici");

        var mood2 = Mood.Create("hype", "#FF5733", null, 1);
        mood2.AddOrUpdateTranslation("tr", "Heyecanlı", "Adrenalin dolu");

        _moodRepositoryMock.Setup(x => x.GetActiveMoodsWithTranslationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([mood1, mood2]);

        var result = await _handler.Handle(new GetActiveMoodsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Code.Should().Be("hype");
        result[1].Code.Should().Be("chill");
    }
}
