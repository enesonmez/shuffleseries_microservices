namespace ShuffleSeries.Shared.Core.Domain.Primitives;

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; }
    string? CreatedBy { get; }
    DateTime? ModifiedAtUtc { get; }
    string? ModifiedBy { get; }
}
