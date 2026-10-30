using Moq;
using ShuffleSeries.Catalog.Application.Features.Moods.Queries.GetMoodById;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Moods;

public class GetMoodByIdQueryHandlerTests
{
    private readonly Mock<IMoodRepository> _moodRepositoryMock;
    private readonly Mock<ILanguageContext> _languageContextMock;
    private readonly GetMoodByIdQueryHandler _handler;

    public GetMoodByIdQueryHandlerTests()
    {
        _moodRepositoryMock = new Mock<IMoodRepository>();
        _languageContextMock = new Mock<ILanguageContext>();
        _languageContextMock.Setup(x => x.CurrentLanguage).Returns("tr");
        _handler = new GetMoodByIdQueryHandler(_moodRepositoryMock.Object, _languageContextMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowMoodNotFoundException_When_MoodDoesNotExist()
    {
        // Arrange
        var query = new GetMoodByIdQuery(Guid.NewGuid());
        _moodRepositoryMock.Setup(x => x.GetByIdForDetailAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mood?)null);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MoodNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnMoodResponse_When_MoodExists()
    {
        // Arrange
        var mood = Mood.Create("kafa-dagitmalik", "#FF0000");
        mood.AddOrUpdateTranslation("tr", "Kafa Dağıtmalık", "Açıklama");
        var query = new GetMoodByIdQuery(mood.Id);

        _moodRepositoryMock.Setup(x => x.GetByIdForDetailAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mood);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(mood.Id);
        result.Code.Should().Be("kafa-dagitmalik");
        result.Name.Should().Be("Kafa Dağıtmalık");
    }
}
