namespace ShuffleSeries.Shared.Core.Domain.Primitives;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAtUtc { get; }
    string? DeletedBy { get; }

    void SoftDelete(string? deletedBy = null, DateTime? deletedAtUtc = null);
    void UndoSoftDelete();
}
