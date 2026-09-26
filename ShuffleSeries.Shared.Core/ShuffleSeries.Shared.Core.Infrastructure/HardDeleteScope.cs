namespace ShuffleSeries.Shared.Core.Infrastructure;

/// <summary>
/// Çevresel fiziksel silme (Hard Delete) kapsamı yöneticisi.
/// AsyncLocal üzerinden asenkron akış boyunca fiziksel silme niyetini taşır.
/// İç içe (nested) kapsamları güvenli bir şekilde destekler.
/// </summary>
public static class HardDeleteScope
{
    private static readonly AsyncLocal<bool> _isHardDeleteActive = new();

    public static bool IsActive => _isHardDeleteActive.Value;

    public static IDisposable Begin()
    {
        var previousValue = _isHardDeleteActive.Value;
        _isHardDeleteActive.Value = true;
        return new ScopeDisposable(previousValue);
    }

    private sealed class ScopeDisposable : IDisposable
    {
        private readonly bool _previousValue;
        private bool _disposed;

        public ScopeDisposable(bool previousValue)
        {
            _previousValue = previousValue;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _isHardDeleteActive.Value = _previousValue;
                _disposed = true;
            }
        }
    }
}
