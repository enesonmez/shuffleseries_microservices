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
        string? imdbId,
        double? imdbRating,
        int? imdbVotes,
        int releaseYear,
        int runtimeMinutes,
        string? director,
        string posterUrl,
        string? backdropUrl)
        : base(id, MediaType.Movie, imdbId, imdbRating, imdbVotes, releaseYear, posterUrl, backdropUrl)
    {
        if (runtimeMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(runtimeMinutes), "Runtime minutes must be greater than zero.");

        RuntimeMinutes = runtimeMinutes;
        Director = string.IsNullOrWhiteSpace(director) ? null : director.Trim();
    }

    public static Movie Create(
        int releaseYear,
        int runtimeMinutes,
        string posterUrl,
        string? backdropUrl = null,
        string? imdbId = null,
        double? imdbRating = null,
        int? imdbVotes = null,
        string? director = null)
    {
        var movie = new Movie(
            Guid.NewGuid(),
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
        int releaseYear,
        int runtimeMinutes,
        string? director,
        string? imdbId,
        double? imdbRating,
        int? imdbVotes)
    {
        if (runtimeMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(runtimeMinutes), "Runtime minutes must be greater than zero.");

        ValidateReleaseYear(releaseYear);
        ValidateRatings(imdbRating, imdbVotes);

        ReleaseYear = releaseYear;
        RuntimeMinutes = runtimeMinutes;
        Director = string.IsNullOrWhiteSpace(director) ? null : director.Trim();
        ImdbId = string.IsNullOrWhiteSpace(imdbId) ? null : imdbId.Trim();
        ImdbRating = imdbRating;
        ImdbVotes = imdbVotes;
        ModifiedAtUtc = DateTime.UtcNow;

        RaiseUpdatedEvent();
    }
}
