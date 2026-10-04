namespace ShuffleSeries.Shared.Core.Domain.Constants;

public static class SystemRoles
{
    public const string Guest = "Guest";
    public const string Standard = "Standard";
    public const string Premium = "Premium";
    public const string Admin = "Admin";

    public static readonly IReadOnlyCollection<string> All =
    [
        Guest,
        Standard,
        Premium,
        Admin
    ];
}
