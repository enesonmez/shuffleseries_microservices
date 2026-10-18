using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class MediaGenre : BaseEntity
{
    public Guid MediaId { get; private set; }
    public Guid GenreId { get; private set; }

    public Genre? Genre { get; private set; }

    private MediaGenre() : base(Guid.Empty) { }

    internal MediaGenre(Guid mediaId, Guid genreId)
        : base(Guid.Empty)
    {
        if (genreId == Guid.Empty)
            throw new ArgumentException("GenreId cannot be empty.", nameof(genreId));

        MediaId = mediaId;
        GenreId = genreId;
    }
}
