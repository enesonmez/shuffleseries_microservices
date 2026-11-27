using ShuffleSeries.Shared.Core.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Application.Common.Models;
using ShuffleSeries.ShuffleEngine.Domain.Models;

namespace ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;

public interface IUserShuffleStateStore
{
    Task AddToCooldownAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>> GetActiveCooldownIdsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserFeatureVector> GetFeatureVectorAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ApplyMediaRatingAffinityAsync(
        Guid userId,
        ShuffleMediaAffinityContext media,
        MediaRatingTier tier,
        CancellationToken cancellationToken = default);

    Task ApplyOnboardingPreferencesAsync(
        Guid userId,
        IReadOnlyCollection<Guid> moodIds,
        CancellationToken cancellationToken = default);

    Task<bool> SetEntitlementAsync(UserShuffleEntitlement entitlement, CancellationToken cancellationToken = default);

    Task<UserShuffleEntitlement?> GetEntitlementAsync(Guid userId, CancellationToken cancellationToken = default);

    Task PurgeUserStateAsync(Guid userId, CancellationToken cancellationToken = default);
}
