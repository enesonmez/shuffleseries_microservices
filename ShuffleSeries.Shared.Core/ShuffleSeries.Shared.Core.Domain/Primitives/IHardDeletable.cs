namespace ShuffleSeries.Shared.Core.Domain.Primitives;

public interface IHardDeletable
{
    bool IsHardDeleteRequested { get; }
    void HardDelete();
}
