namespace ShuffleSeries.Shared.Core.Domain.Constants;

public static class SystemPolicies
{
    public const string RequirePremiumRole = "RequirePremiumRole";
    public const string RequireAdminRole = "RequireAdminRole";

    public const string CanReadCatalog = "CanReadCatalog";
    public const string CanCreateCatalog = "CanCreateCatalog";
    public const string CanUpdateCatalog = "CanUpdateCatalog";
    public const string CanDeleteCatalog = "CanDeleteCatalog";

    public const string CanExecuteBasicShuffle = "CanExecuteBasicShuffle";
    public const string CanExecuteVipShuffle = "CanExecuteVipShuffle";

    public const string CanManageAuth = "CanManageAuth";
}
