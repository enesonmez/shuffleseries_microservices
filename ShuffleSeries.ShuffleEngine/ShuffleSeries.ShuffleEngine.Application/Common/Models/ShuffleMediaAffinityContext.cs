namespace ShuffleSeries.ShuffleEngine.Application.Common.Models;

public sealed record ShuffleMediaAffinityContext(
    Guid MediaId,
    IReadOnlyList<Guid> MoodIds,
    IReadOnlyList<Guid> GenreIds,
    string? Director);
