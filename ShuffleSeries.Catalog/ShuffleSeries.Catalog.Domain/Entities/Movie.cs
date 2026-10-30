using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Events;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class Movie : Media
{
    public int RuntimeMinutes { get; private set; }
    public string? Director { get; private set; }

    private Movie() : base()
    {
        MediaType = MediaType.Movie;
    }

    private Movie(
        Guid id,
        int tmdbId,
        string imdbId,
        double? imdbRating,
        int? imdbVotes,
        int releaseYear,
        int runtimeMinutes,
        string? director,
        string posterUrl,
        string? backdropUrl)
        : base(id, MediaType.Movie, tmdbId, imdbId, imdbRating, imdbVotes, releaseYear, posterUrl, backdropUrl)
    {
        if (runtimeMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(runtimeMinutes), "Runtime minutes must be greater than zero.");

        RuntimeMinutes = runtimeMinutes;
        Director = string.IsNullOrWhiteSpace(director) ? null : director.Trim();
    }

    public static Movie Create(
        int tmdbId,
        string imdbId,
        int releaseYear,
        int runtimeMinutes,
        string posterUrl,
        string? backdropUrl = null,
        double? imdbRating = null,
        int? imdbVotes = null,
        string? director = null)
    {
        var movie = new Movie(
            Guid.NewGuid(),
            tmdbId,
            imdbId,
            imdbRating,
            imdbVotes,
            releaseYear,
            runtimeMinutes,
            director,
            posterUrl,
            backdropUrl);

        movie.RaiseDomainEvent(new MediaCreatedDomainEvent(movie.Id, MediaType.Movie));

        return movie;
    }

    public void UpdateDetails(
        int tmdbId,
        string imdbId,
        int releaseYear,
        int runtimeMinutes,
        string? director,
        double? imdbRating,
        int? imdbVotes,
        string? posterUrl = null,
        string? backdropUrl = null)
    {
        if (tmdbId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tmdbId), "TMDb ID must be greater than zero.");

        if (string.IsNullOrWhiteSpace(imdbId))
            throw new ArgumentException("IMDb ID cannot be empty.", nameof(imdbId));

        if (runtimeMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(runtimeMinutes), "Runtime minutes must be greater than zero.");

        ValidateReleaseYear(releaseYear);
        ValidateRatings(imdbRating, imdbVotes);

        TmdbId = tmdbId;
        ImdbId = imdbId.Trim();
        ReleaseYear = releaseYear;
        RuntimeMinutes = runtimeMinutes;
        Director = string.IsNullOrWhiteSpace(director) ? null : director.Trim();
        ImdbRating = imdbRating;
        ImdbVotes = imdbVotes;

        if (!string.IsNullOrWhiteSpace(posterUrl))
            PosterUrl = posterUrl.Trim();

        if (backdropUrl is not null)
            BackdropUrl = string.IsNullOrWhiteSpace(backdropUrl) ? null : backdropUrl.Trim();

        RaiseUpdatedEvent();
    }
}
