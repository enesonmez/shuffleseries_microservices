using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Genres.Queries.GetActiveGenres;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Genres;

public class GetActiveGenresQueryHandlerTests
{
    private readonly Mock<IGenreRepository> _genreRepositoryMock = new();
    private readonly Mock<ICatalogCacheService> _cacheServiceMock = new();
    private readonly Mock<ILanguageContext> _languageContextMock = new();
    private readonly GetActiveGenresQueryHandler _handler;

    public GetActiveGenresQueryHandlerTests()
    {
        _languageContextMock.Setup(x => x.CurrentLanguage).Returns("tr");

        // Mock cache to directly invoke factory
        _cacheServiceMock.Setup(x => x.GetOrCreateAsync(
                CatalogCacheKeys.Genres,
                It.IsAny<Func<Task<List<Genre>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<List<Genre>>>, TimeSpan?, CancellationToken>(
                (_, factory, _, _) => factory());

        _handler = new GetActiveGenresQueryHandler(
            _genreRepositoryMock.Object,
            _cacheServiceMock.Object,
            _languageContextMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnActiveGenresOrderedByDisplayOrder()
    {
        var genre1 = Genre.Create("comedy", 2);
        genre1.AddOrUpdateTranslation("tr", "Komedi", "Eğlenceli içerikler");

        var genre2 = Genre.Create("action", 1);
        genre2.AddOrUpdateTranslation("tr", "Aksiyon", "Hareketli içerikler");

        _genreRepositoryMock.Setup(x => x.GetActiveGenresWithTranslationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([genre1, genre2]);

        var result = await _handler.Handle(new GetActiveGenresQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Code.Should().Be("action");
        result[1].Code.Should().Be("comedy");
    }
}
