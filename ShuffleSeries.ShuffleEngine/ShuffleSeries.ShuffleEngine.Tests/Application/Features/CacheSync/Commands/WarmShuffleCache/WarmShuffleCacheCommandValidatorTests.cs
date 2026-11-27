using FluentValidation.TestHelper;
using ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Commands.WarmShuffleCache;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Features.CacheSync.Commands.WarmShuffleCache;

public class WarmShuffleCacheCommandValidatorTests
{
    private readonly WarmShuffleCacheCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenTriggerReasonIsValidOrNull_ShouldHaveNoErrors()
    {
        _validator.TestValidate(new WarmShuffleCacheCommand(false, null)).IsValid.Should().BeTrue();
        _validator.TestValidate(new WarmShuffleCacheCommand(true, "Admin manual sync")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenTriggerReasonExceeds100CharsOrContainsNullByte_ShouldHaveError()
    {
        var tooLong = new WarmShuffleCacheCommand(false, new string('x', 101));
        _validator.TestValidate(tooLong).ShouldHaveValidationErrorFor(x => x.TriggerReason);

        var nulByte = new WarmShuffleCacheCommand(false, "bad\0reason");
        _validator.TestValidate(nulByte).ShouldHaveValidationErrorFor(x => x.TriggerReason);
    }
}
