using Moq;
using ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetMediaByIdForShuffle;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.ShuffleSync.Queries.GetMediaByIdForShuffle;

public class GetMediaByIdForShuffleQueryHandlerTests
{
    private readonly Mock<IMediaRepository> _mediaRepositoryMock = new();
    private readonly GetMediaByIdForShuffleQueryHandler _handler;

    public GetMediaByIdForShuffleQueryHandlerTests()
    {
        _handler = new GetMediaByIdForShuffleQueryHandler(_mediaRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowMediaNotFoundException_When_MediaDoesNotExist()
    {
        var mediaId = Guid.NewGuid();
        _mediaRepositoryMock
            .Setup(x => x.GetByIdForShuffleAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Media?)null);

        var action = async () => await _handler.Handle(new GetMediaByIdForShuffleQuery(mediaId), CancellationToken.None);

        await action.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnShuffleMediaItemResponse_When_MediaExists()
    {
        var movie = Movie.Create(27205, "tt1375666", 2010, 148, "https://example.com/inception.jpg", imdbRating: 8.8, director: "Christopher Nolan");
        movie.AddOrUpdateTranslation("tr", "Başlangıç", "Rüya paylaşımı");
        movie.AddOrUpdateTranslation("en", "Inception", "Dream sharing");

        _mediaRepositoryMock
            .Setup(x => x.GetByIdForShuffleAsync(movie.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        var result = await _handler.Handle(new GetMediaByIdForShuffleQuery(movie.Id), CancellationToken.None);

        result.Id.Should().Be(movie.Id);
        result.MediaType.Should().Be(MediaType.Movie);
        result.TmdbId.Should().Be(27205);
        result.ImdbId.Should().Be("tt1375666");
        result.Translations.Should().ContainKeys("tr", "en");
    }
}
