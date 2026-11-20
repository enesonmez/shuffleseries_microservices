using ShuffleSeries.Catalog.Application.Common.Models;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Common.Services;

public static class MediaTaxonomyApplier
{
    public static async Task ApplyAsync(
        Media media,
        MediaTaxonomySelection selection,
        IPlatformRepository platformRepository,
        IMoodRepository moodRepository,
        IGenreRepository genreRepository,
        CancellationToken cancellationToken = default)
    {
        await ApplyPlatformsAsync(media, selection.PlatformIds, platformRepository, cancellationToken);
        await ApplyMoodsAsync(media, selection.MoodIds, moodRepository, cancellationToken);
        await ApplyGenresAsync(media, selection.GenreIds, genreRepository, cancellationToken);
    }

    private static async Task ApplyPlatformsAsync(
        Media media,
        IReadOnlyList<Guid>? platformIds,
        IPlatformRepository platformRepository,
        CancellationToken cancellationToken)
    {
        if (platformIds is not { Count: > 0 })
        {
            return;
        }

        var distinctPlatformIds = platformIds.Distinct().ToList();
        var activePlatforms = (await platformRepository.GetActivePlatformsAsync(cancellationToken))
            .ToDictionary(p => p.Id);

        foreach (var platformId in distinctPlatformIds)
        {
            if (!activePlatforms.TryGetValue(platformId, out var platform))
            {
                throw new PlatformNotFoundException(platformId);
            }

            media.AddPlatform(platform.Id, platform.BaseWebUrl);
        }
    }

    private static async Task ApplyMoodsAsync(
        Media media,
        IReadOnlyList<Guid>? moodIds,
        IMoodRepository moodRepository,
        CancellationToken cancellationToken)
    {
        if (moodIds is not { Count: > 0 })
        {
            return;
        }

        var distinctMoodIds = moodIds.Distinct().ToList();
        var existingMoodIds = (await moodRepository.GetExistingActiveIdsAsync(distinctMoodIds, cancellationToken))
            .ToHashSet();

        foreach (var moodId in distinctMoodIds)
        {
            if (!existingMoodIds.Contains(moodId))
            {
                throw new MoodNotFoundException(moodId);
            }

            media.AddMood(moodId);
        }
    }

    private static async Task ApplyGenresAsync(
        Media media,
        IReadOnlyList<Guid>? genreIds,
        IGenreRepository genreRepository,
        CancellationToken cancellationToken)
    {
        if (genreIds is not { Count: > 0 })
        {
            return;
        }

        var distinctGenreIds = genreIds.Distinct().ToList();
        var existingGenreIds = (await genreRepository.GetExistingActiveIdsAsync(distinctGenreIds, cancellationToken))
            .ToHashSet();

        foreach (var genreId in distinctGenreIds)
        {
            if (!existingGenreIds.Contains(genreId))
            {
                throw new GenreNotFoundException(genreId);
            }

            media.AddGenre(genreId);
        }
    }
}
