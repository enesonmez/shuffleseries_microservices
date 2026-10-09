namespace ShuffleSeries.Shared.Core.Domain.Constants;

public static class SystemPermissions
{
    public static class Catalog
    {
        public const string Read = "catalog:read";
        public const string Create = "catalog:create";
        public const string Update = "catalog:update";
        public const string Delete = "catalog:delete";
    }

    public static class Shuffle
    {
        public const string Basic = "shuffle:basic";
        public const string Vip = "shuffle:vip";
    }

    public static class Auth
    {
        public const string Manage = "auth:manage";
    }

    public const string CatalogRead = Catalog.Read;
    public const string CatalogCreate = Catalog.Create;
    public const string CatalogUpdate = Catalog.Update;
    public const string CatalogDelete = Catalog.Delete;
    public const string ShuffleBasic = Shuffle.Basic;
    public const string ShuffleVip = Shuffle.Vip;
    public const string AuthManage = Auth.Manage;

    public static readonly IReadOnlyCollection<string> All =
    [
        Catalog.Read,
        Catalog.Create,
        Catalog.Update,
        Catalog.Delete,
        Shuffle.Basic,
        Shuffle.Vip,
        Auth.Manage
    ];
}
