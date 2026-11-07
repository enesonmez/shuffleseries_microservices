using Moq;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Translations.AddOrUpdateMediaTranslation;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.MediaAssociations.Commands.Translations.AddOrUpdateMediaTranslation;

public class AddOrUpdateMediaTranslationCommandHandlerTests
{
    private readonly Mock<IMediaRepository> _mediaRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task Handle_ShouldThrowMediaNotFoundException_WhenMediaDoesNotExist()
    {
        var handler = new AddOrUpdateMediaTranslationCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var command = new AddOrUpdateMediaTranslationCommand(Guid.NewGuid(), "tr", "Yeni Başlık", "Özet", "Slogan");

        _mediaRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Media?)null);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldAddTranslationAndSave_WhenMediaExists()
    {
        var handler = new AddOrUpdateMediaTranslationCommandHandler(_mediaRepositoryMock.Object, _unitOfWorkMock.Object);
        var movie = Movie.Create(550, "tt0137523", 2020, 100, "https://example.com/p.jpg");
        var command = new AddOrUpdateMediaTranslationCommand(movie.Id, "tr", "Yeni Başlık", "Özet", "Slogan");

        _mediaRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.MediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        await handler.Handle(command, CancellationToken.None);

        movie.Translations.Should().ContainSingle(t => t.Title == "Yeni Başlık" && t.LanguageCode.Value == "tr");
        _mediaRepositoryMock.Verify(x => x.Update(movie), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
