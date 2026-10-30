namespace ShuffleSeries.Shared.Core.Domain.Constants;

public static class SystemPolicies
{
    public const string RequirePremiumRole = "RequirePremiumRole";
    public const string RequireAdminRole = "RequireAdminRole";

    // Granular Movies Policies
    public const string CanReadCatalogMovies = "CanReadCatalogMovies";
    public const string CanCreateCatalogMovies = "CanCreateCatalogMovies";
    public const string CanUpdateCatalogMovies = "CanUpdateCatalogMovies";
    public const string CanDeleteCatalogMovies = "CanDeleteCatalogMovies";

    // Granular Series Policies
    public const string CanReadCatalogSeries = "CanReadCatalogSeries";
    public const string CanCreateCatalogSeries = "CanCreateCatalogSeries";
    public const string CanUpdateCatalogSeries = "CanUpdateCatalogSeries";
    public const string CanDeleteCatalogSeries = "CanDeleteCatalogSeries";

    // Granular Episodes Policies
    public const string CanReadCatalogEpisodes = "CanReadCatalogEpisodes";
    public const string CanCreateCatalogEpisodes = "CanCreateCatalogEpisodes";
    public const string CanUpdateCatalogEpisodes = "CanUpdateCatalogEpisodes";
    public const string CanDeleteCatalogEpisodes = "CanDeleteCatalogEpisodes";

    // Granular Platforms Policies
    public const string CanReadCatalogPlatforms = "CanReadCatalogPlatforms";
    public const string CanCreateCatalogPlatforms = "CanCreateCatalogPlatforms";
    public const string CanUpdateCatalogPlatforms = "CanUpdateCatalogPlatforms";
    public const string CanDeleteCatalogPlatforms = "CanDeleteCatalogPlatforms";

    // Granular Moods Policies
    public const string CanReadCatalogMoods = "CanReadCatalogMoods";
    public const string CanCreateCatalogMoods = "CanCreateCatalogMoods";
    public const string CanUpdateCatalogMoods = "CanUpdateCatalogMoods";
    public const string CanDeleteCatalogMoods = "CanDeleteCatalogMoods";

    // Granular Genres Policies
    public const string CanReadCatalogGenres = "CanReadCatalogGenres";
    public const string CanCreateCatalogGenres = "CanCreateCatalogGenres";
    public const string CanUpdateCatalogGenres = "CanUpdateCatalogGenres";
    public const string CanDeleteCatalogGenres = "CanDeleteCatalogGenres";

    // Granular Media Associations & Translations
    public const string CanManageCatalogAssociations = "CanManageCatalogAssociations";

    // Admin CMS Policies
    public const string CanReadAdminMedia = "CanReadAdminMedia";

    // Shuffle Policies
    public const string CanExecuteBasicShuffle = "CanExecuteBasicShuffle";
    public const string CanExecuteVipShuffle = "CanExecuteVipShuffle";

    // Auth Policies
    public const string CanManageAuth = "CanManageAuth";
}
