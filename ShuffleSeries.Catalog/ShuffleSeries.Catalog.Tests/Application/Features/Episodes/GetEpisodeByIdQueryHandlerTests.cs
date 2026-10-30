using Moq;
using ShuffleSeries.Catalog.Application.Features.Episodes.Queries.GetEpisodeById;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes;

public class GetEpisodeByIdQueryHandlerTests
{
    private readonly Mock<IEpisodeRepository> _episodeRepositoryMock = new();
    private readonly Mock<ILanguageContext> _languageContextMock = new();
    private readonly GetEpisodeByIdQueryHandler _handler;

    public GetEpisodeByIdQueryHandlerTests()
    {
        _languageContextMock.Setup(x => x.CurrentLanguage).Returns("tr");
        _handler = new GetEpisodeByIdQueryHandler(_episodeRepositoryMock.Object, _languageContextMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_EpisodeDoesNotExist()
    {
        var query = new GetEpisodeByIdQuery(Guid.NewGuid());

        _episodeRepositoryMock.Setup(x => x.GetByIdForDetailAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Episode?)null);

        var action = async () => await _handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<EpisodeNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnEpisodeDetailResponse_When_EpisodeExists()
    {
        var episode = new Episode(Guid.NewGuid(), 1, isStandalone: true, runtimeMinutes: 45, imdbRating: 9.5);
        episode.AddOrUpdateTranslation("tr", "Göz Kırpma", "Gözünü kırparsan ölürsün");

        var query = new GetEpisodeByIdQuery(episode.Id);

        _episodeRepositoryMock.Setup(x => x.GetByIdForDetailAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(episode);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(episode.Id);
        result.EpisodeNumber.Should().Be(1);
        result.IsStandalone.Should().BeTrue();
        result.RuntimeMinutes.Should().Be(45);
        result.ImdbRating.Should().Be(9.5);
        result.Title.Should().Be("Göz Kırpma");
        result.Overview.Should().Be("Gözünü kırparsan ölürsün");
    }
}
