using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.UpdateEpisode;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes;

public class UpdateEpisodeCommandValidatorTests
{
    private readonly UpdateEpisodeCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        var command = new UpdateEpisodeCommand(
            Id: Guid.NewGuid(),
            EpisodeNumber: 3,
            IsStandalone: true,
            RuntimeMinutes: 45,
            ImdbRating: 8.7,
            ThumbnailUrl: "https://example.com/ep3.jpg");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        var command = new UpdateEpisodeCommand(Guid.Empty, 1, false, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Fail_When_EpisodeNumberIsZeroOrNegative(int ep)
    {
        var command = new UpdateEpisodeCommand(Guid.NewGuid(), ep, false, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.EpisodeNumber);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(10.1)]
    public void Should_Fail_When_ImdbRatingIsOutOfBounds(double rating)
    {
        var command = new UpdateEpisodeCommand(Guid.NewGuid(), 1, false, 50, rating, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ImdbRating);
    }
}
