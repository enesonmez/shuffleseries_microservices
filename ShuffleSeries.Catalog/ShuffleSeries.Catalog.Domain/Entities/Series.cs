using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Events;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class Series : Media
{
    private readonly List<Season> _seasons = [];

    public SeriesType SeriesType { get; private set; }
    public int TotalSeasons { get; private set; }

    public IReadOnlyCollection<Season> Seasons => _seasons.AsReadOnly();

    private Series() : base()
    {
        MediaType = MediaType.Series;
    }

    private Series(
        Guid id,
        string? imdbId,
        double? imdbRating,
        int? imdbVotes,
        int releaseYear,
        SeriesType seriesType,
        int totalSeasons,
        string posterUrl,
        string? backdropUrl)
        : base(id, MediaType.Series, imdbId, imdbRating, imdbVotes, releaseYear, posterUrl, backdropUrl)
    {
        SeriesType = seriesType;
        TotalSeasons = totalSeasons >= 0 ? totalSeasons : 0;
    }

    public static Series Create(
        int releaseYear,
        SeriesType seriesType,
        string posterUrl,
        string? backdropUrl = null,
        string? imdbId = null,
        double? imdbRating = null,
        int? imdbVotes = null,
        int totalSeasons = 0)
    {
        var series = new Series(
            Guid.NewGuid(),
            imdbId,
            imdbRating,
            imdbVotes,
            releaseYear,
            seriesType,
            totalSeasons,
            posterUrl,
            backdropUrl);

        series.RaiseDomainEvent(new MediaCreatedDomainEvent(series.Id, MediaType.Series));

        return series;
    }

    public void UpdateDetails(
        int releaseYear,
        SeriesType seriesType,
        int totalSeasons,
        string? imdbId,
        double? imdbRating,
        int? imdbVotes)
    {
        ValidateReleaseYear(releaseYear);
        ValidateRatings(imdbRating, imdbVotes);

        ReleaseYear = releaseYear;
        SeriesType = seriesType;
        TotalSeasons = totalSeasons >= 0 ? totalSeasons : 0;
        ImdbId = string.IsNullOrWhiteSpace(imdbId) ? null : imdbId.Trim();
        ImdbRating = imdbRating;
        ImdbVotes = imdbVotes;
        ModifiedAtUtc = DateTime.UtcNow;

        RaiseUpdatedEvent();
    }

    public Season AddSeason(int seasonNumber, string? title = null)
    {
        var existing = _seasons.FirstOrDefault(s => s.SeasonNumber == seasonNumber);
        if (existing is not null)
            return existing;

        var season = new Season(Id, seasonNumber, title);
        _seasons.Add(season);

        if (seasonNumber > TotalSeasons)
            TotalSeasons = seasonNumber;

        ModifiedAtUtc = DateTime.UtcNow;
        RaiseUpdatedEvent();
        return season;
    }

    public Episode AddEpisode(
        int seasonNumber,
        int episodeNumber,
        bool? isStandalone = null,
        int? runtimeMinutes = null,
        double? imdbRating = null,
        string? thumbnailUrl = null)
    {
        var season = AddSeason(seasonNumber);

        // Eğer isStandalone açıkça verilmediyse, EpisodicAnthology dizilerinde otomatik true, diğerlerinde false
        var standaloneFlag = isStandalone ?? (SeriesType == SeriesType.EpisodicAnthology);

        var episode = season.AddEpisode(
            episodeNumber,
            standaloneFlag,
            runtimeMinutes,
            imdbRating,
            thumbnailUrl);

        ModifiedAtUtc = DateTime.UtcNow;
        RaiseUpdatedEvent();
        return episode;
    }
}
