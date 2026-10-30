namespace ShuffleSeries.Shared.Core.Domain.Constants;

public static class SystemPermissions
{
    public static class Catalog
    {
        // Movies granular permissions
        public const string MoviesRead = "catalog:movies:read";
        public const string MoviesCreate = "catalog:movies:create";
        public const string MoviesUpdate = "catalog:movies:update";
        public const string MoviesDelete = "catalog:movies:delete";

        // Series granular permissions
        public const string SeriesRead = "catalog:series:read";
        public const string SeriesCreate = "catalog:series:create";
        public const string SeriesUpdate = "catalog:series:update";
        public const string SeriesDelete = "catalog:series:delete";

        // Episodes granular permissions
        public const string EpisodesRead = "catalog:episodes:read";
        public const string EpisodesCreate = "catalog:episodes:create";
        public const string EpisodesUpdate = "catalog:episodes:update";
        public const string EpisodesDelete = "catalog:episodes:delete";

        // Platforms granular permissions
        public const string PlatformsRead = "catalog:platforms:read";
        public const string PlatformsCreate = "catalog:platforms:create";
        public const string PlatformsUpdate = "catalog:platforms:update";
        public const string PlatformsDelete = "catalog:platforms:delete";

        // Moods granular permissions
        public const string MoodsRead = "catalog:moods:read";
        public const string MoodsCreate = "catalog:moods:create";
        public const string MoodsUpdate = "catalog:moods:update";
        public const string MoodsDelete = "catalog:moods:delete";

        // Genres granular permissions
        public const string GenresRead = "catalog:genres:read";
        public const string GenresCreate = "catalog:genres:create";
        public const string GenresUpdate = "catalog:genres:update";
        public const string GenresDelete = "catalog:genres:delete";

        // Media associations & translations
        public const string AssociationsManage = "catalog:associations:manage";

        // Admin CMS
        public const string AdminMediaRead = "catalog:admin:read";
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

    public const string ShuffleBasic = Shuffle.Basic;
    public const string ShuffleVip = Shuffle.Vip;
    public const string AuthManage = Auth.Manage;

    public static readonly IReadOnlyCollection<string> All =
    [
        Catalog.MoviesRead,
        Catalog.MoviesCreate,
        Catalog.MoviesUpdate,
        Catalog.MoviesDelete,
        Catalog.SeriesRead,
        Catalog.SeriesCreate,
        Catalog.SeriesUpdate,
        Catalog.SeriesDelete,
        Catalog.EpisodesRead,
        Catalog.EpisodesCreate,
        Catalog.EpisodesUpdate,
        Catalog.EpisodesDelete,
        Catalog.PlatformsRead,
        Catalog.PlatformsCreate,
        Catalog.PlatformsUpdate,
        Catalog.PlatformsDelete,
        Catalog.MoodsRead,
        Catalog.MoodsCreate,
        Catalog.MoodsUpdate,
        Catalog.MoodsDelete,
        Catalog.GenresRead,
        Catalog.GenresCreate,
        Catalog.GenresUpdate,
        Catalog.GenresDelete,
        Catalog.AssociationsManage,
        Catalog.AdminMediaRead,
        Shuffle.Basic,
        Shuffle.Vip,
        Auth.Manage
    ];
}
