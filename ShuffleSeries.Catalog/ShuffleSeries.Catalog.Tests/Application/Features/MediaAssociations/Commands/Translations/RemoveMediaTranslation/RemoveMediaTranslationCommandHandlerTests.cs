using Moq;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Translations.RemoveMediaTranslation;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Tests.Application.Features.MediaAssociations.Commands.Translations.RemoveMediaTranslation;

public class RemoveMediaTranslationCommandHandlerTests
{
    private readonly Mock<IMediaRepository> _mediaRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task Handle_ShouldRemoveTranslationAndSave_WhenMediaExists()
    {
        var handler = new RemoveMediaTranslationCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var movie = Movie.Create(550, "tt0137523", 2020, 100, "https://example.com/p.jpg");
        movie.AddOrUpdateTranslation(LanguageCode.Turkish, "Türkçe", "Özet");
        movie.AddOrUpdateTranslation(LanguageCode.English, "English", "Overview");

        var command = new RemoveMediaTranslationCommand(movie.Id, "en");

        _mediaRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        await handler.Handle(command, CancellationToken.None);

        movie.Translations.Should().NotContain(t => t.LanguageCode == LanguageCode.English);
        _mediaRepositoryMock.Verify(x => x.Update(movie), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowMediaNotFoundException_WhenMediaDoesNotExist()
    {
        var handler = new RemoveMediaTranslationCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var command = new RemoveMediaTranslationCommand(Guid.NewGuid(), "tr");

        _mediaRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Media?)null);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MediaNotFoundException>();
    }
}
