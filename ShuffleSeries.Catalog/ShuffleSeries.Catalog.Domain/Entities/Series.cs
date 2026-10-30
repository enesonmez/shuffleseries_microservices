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
        int tmdbId,
        string imdbId,
        double? imdbRating,
        int? imdbVotes,
        int releaseYear,
        SeriesType seriesType,
        int totalSeasons,
        string posterUrl,
        string? backdropUrl)
        : base(id, MediaType.Series, tmdbId, imdbId, imdbRating, imdbVotes, releaseYear, posterUrl, backdropUrl)
    {
        SeriesType = seriesType;
        TotalSeasons = totalSeasons >= 0 ? totalSeasons : 0;
    }

    public static Series Create(
        int tmdbId,
        string imdbId,
        int releaseYear,
        SeriesType seriesType,
        string posterUrl,
        string? backdropUrl = null,
        double? imdbRating = null,
        int? imdbVotes = null,
        int totalSeasons = 0)
    {
        var series = new Series(
            Guid.NewGuid(),
            tmdbId,
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
        int tmdbId,
        string imdbId,
        int releaseYear,
        SeriesType seriesType,
        int totalSeasons,
        double? imdbRating,
        int? imdbVotes,
        string? posterUrl = null,
        string? backdropUrl = null)
    {
        if (tmdbId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tmdbId), "TMDb ID must be greater than zero.");

        if (string.IsNullOrWhiteSpace(imdbId))
            throw new ArgumentException("IMDb ID cannot be empty.", nameof(imdbId));

        ValidateReleaseYear(releaseYear);
        ValidateRatings(imdbRating, imdbVotes);

        TmdbId = tmdbId;
        ImdbId = imdbId.Trim();
        ReleaseYear = releaseYear;
        SeriesType = seriesType;
        TotalSeasons = totalSeasons >= 0 ? totalSeasons : 0;
        ImdbRating = imdbRating;
        ImdbVotes = imdbVotes;

        if (!string.IsNullOrWhiteSpace(posterUrl))
            PosterUrl = posterUrl.Trim();

        if (backdropUrl is not null)
            BackdropUrl = string.IsNullOrWhiteSpace(backdropUrl) ? null : backdropUrl.Trim();

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

        RaiseUpdatedEvent();
        RaiseDomainEvent(new EpisodeCreatedDomainEvent(Id, season.Id, episode.Id, episodeNumber, standaloneFlag));
        return episode;
    }

    public override void Delete(string? deletedBy = null)
    {
        base.Delete(deletedBy);
        foreach (var season in _seasons)
        {
            season.Delete(deletedBy);
        }
    }
}
