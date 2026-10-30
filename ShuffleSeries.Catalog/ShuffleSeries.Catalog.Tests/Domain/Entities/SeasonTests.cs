using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;

namespace ShuffleSeries.Catalog.Tests.Domain.Entities;

public class SeasonTests
{
    [Fact]
    public void Constructor_Should_CreateSeason_When_ValidParameters()
    {
        var seriesId = Guid.NewGuid();
        var season = new Season(seriesId, 1, " Season 1 ");

        season.SeriesId.Should().Be(seriesId);
        season.SeasonNumber.Should().Be(1);
        season.Title.Should().Be("Season 1");
        season.Episodes.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_Should_ThrowArgumentOutOfRangeException_When_SeasonNumberIsInvalid(int invalidSeasonNumber)
    {
        var seriesId = Guid.NewGuid();
        var action = () => new Season(seriesId, invalidSeasonNumber);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Season number must be greater than zero.*");
    }

    [Fact]
    public void AddEpisode_Should_AddEpisodeToSeason()
    {
        var season = new Season(Guid.NewGuid(), 1);

        var episode = season.AddEpisode(1, isStandalone: true, runtimeMinutes: 45, imdbRating: 8.5);

        season.Episodes.Should().ContainSingle();
        episode.EpisodeNumber.Should().Be(1);
        episode.IsStandalone.Should().BeTrue();
        episode.RuntimeMinutes.Should().Be(45);
        episode.ImdbRating.Should().Be(8.5);
    }

    [Fact]
    public void AddEpisode_Should_ThrowDuplicateEpisodeException_When_EpisodeNumberAlreadyExists()
    {
        var season = new Season(Guid.NewGuid(), 1);
        season.AddEpisode(1, isStandalone: false);

        var action = () => season.AddEpisode(1, isStandalone: true);

        action.Should().Throw<DuplicateEpisodeException>();
    }

    [Fact]
    public void Delete_Should_SoftDeleteSeasonAndCascadeSoftDeleteEpisodes()
    {
        var season = new Season(Guid.NewGuid(), 1);
        var episode1 = season.AddEpisode(1, isStandalone: false);
        var episode2 = season.AddEpisode(2, isStandalone: true);

        season.Delete("test-admin");

        season.IsDeleted.Should().BeTrue();
        season.DeletedBy.Should().Be("test-admin");
        season.DeletedAtUtc.Should().NotBeNull();

        episode1.IsDeleted.Should().BeTrue();
        episode1.DeletedBy.Should().Be("test-admin");
        episode1.DeletedAtUtc.Should().NotBeNull();

        episode2.IsDeleted.Should().BeTrue();
        episode2.DeletedBy.Should().Be("test-admin");
        episode2.DeletedAtUtc.Should().NotBeNull();
    }
}
