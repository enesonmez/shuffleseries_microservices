using FluentValidation;

namespace ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Commands.WarmShuffleCache;

public sealed class WarmShuffleCacheCommandValidator : AbstractValidator<WarmShuffleCacheCommand>
{
    public WarmShuffleCacheCommandValidator()
    {
        RuleFor(x => x.TriggerReason)
            .MaximumLength(100).WithMessage("TriggerReason cannot exceed 100 characters.")
            .Must(s => s is null || !s.Contains('\0'))
            .WithMessage("TriggerReason contains invalid control characters.");
    }
}
