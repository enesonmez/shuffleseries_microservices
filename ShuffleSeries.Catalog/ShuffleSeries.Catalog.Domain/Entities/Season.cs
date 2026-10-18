using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class Season : BaseEntity
{
    private readonly List<Episode> _episodes = [];

    public Guid SeriesId { get; private set; }
    public int SeasonNumber { get; private set; }
    public string? Title { get; private set; }

    public IReadOnlyCollection<Episode> Episodes => _episodes.AsReadOnly();

    private Season() : base(Guid.Empty) { }

    internal Season(Guid seriesId, int seasonNumber, string? title = null) : base(Guid.Empty)
    {
        if (seasonNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(seasonNumber), "Season number must be greater than zero.");

        SeriesId = seriesId;
        SeasonNumber = seasonNumber;
        Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
    }

    public Episode AddEpisode(
        int episodeNumber,
        bool isStandalone,
        int? runtimeMinutes = null,
        double? imdbRating = null,
        string? thumbnailUrl = null)
    {
        var existing = _episodes.FirstOrDefault(e => e.EpisodeNumber == episodeNumber);
        if (existing is not null)
            throw new DuplicateEpisodeException(SeasonNumber, episodeNumber);

        var episode = new Episode(
            Id,
            episodeNumber,
            isStandalone,
            runtimeMinutes,
            imdbRating,
            thumbnailUrl);

        _episodes.Add(episode);
        ModifiedAtUtc = DateTime.UtcNow;

        return episode;
    }

    public void UpdateTitle(string? title)
    {
        Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        ModifiedAtUtc = DateTime.UtcNow;
    }
}
