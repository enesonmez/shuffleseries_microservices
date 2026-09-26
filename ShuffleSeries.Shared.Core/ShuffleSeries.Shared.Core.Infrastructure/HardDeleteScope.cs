namespace ShuffleSeries.Shared.Core.Infrastructure;

public static class HardDeleteScope
{
    private static readonly AsyncLocal<bool> _isHardDeleteActive = new();

    public static bool IsActive => _isHardDeleteActive.Value;

    public static IDisposable Begin()
    {
        _isHardDeleteActive.Value = true;
        return new ScopeDisposable();
    }

    private sealed class ScopeDisposable : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _isHardDeleteActive.Value = false;
                _disposed = true;
            }
        }
    }
}
