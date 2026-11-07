using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Common.Services;

public static class MediaTaxonomyApplier
{
    public static async Task ApplyAsync(
        Media media,
        IReadOnlyList<Guid>? platformIds,
        IReadOnlyList<Guid>? moodIds,
        IReadOnlyList<Guid>? genreIds,
        IPlatformRepository platformRepository,
        IMoodRepository moodRepository,
        IGenreRepository genreRepository,
        CancellationToken cancellationToken = default)
    {
        if (platformIds is { Count: > 0 })
        {
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

        if (moodIds is { Count: > 0 })
        {
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

        if (genreIds is { Count: > 0 })
        {
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
}
