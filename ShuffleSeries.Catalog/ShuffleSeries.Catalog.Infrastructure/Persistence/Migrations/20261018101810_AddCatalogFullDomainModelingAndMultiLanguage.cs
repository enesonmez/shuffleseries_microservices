using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogFullDomainModelingAndMultiLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Series",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Series_Title",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "IsIndependentEpisodes",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Series");

            migrationBuilder.RenameTable(
                name: "Series",
                newName: "Media");

            migrationBuilder.RenameIndex(
                name: "IX_Series_IsDeleted",
                table: "Media",
                newName: "IX_Media_IsDeleted");

            migrationBuilder.AddColumn<string>(
                name: "BackdropUrl",
                table: "Media",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Director",
                table: "Media",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImdbId",
                table: "Media",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ImdbRating",
                table: "Media",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImdbVotes",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaType",
                table: "Media",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PosterUrl",
                table: "Media",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReleaseYear",
                table: "Media",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RuntimeMinutes",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeriesType",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalSeasons",
                table: "Media",
                type: "integer",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Media",
                table: "Media",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "MediaTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Overview = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Tagline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaTranslations_Media_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Moods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AmbientColorHex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Moods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Platforms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    InactiveLogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BaseWebUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BaseAppSchemeUrl = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platforms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seasons_Media_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaMoods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaMoods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaMoods_Media_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaMoods_Moods_MoodId",
                        column: x => x.MoodId,
                        principalTable: "Moods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MoodTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    VibeTags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoodTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MoodTranslations_Moods_MoodId",
                        column: x => x.MoodId,
                        principalTable: "Moods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaPlatforms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatformId = table.Column<Guid>(type: "uuid", nullable: false),
                    WebUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AppSchemeUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaPlatforms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaPlatforms_Media_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaPlatforms_Platforms_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "Platforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Episodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    EpisodeNumber = table.Column<int>(type: "integer", nullable: false),
                    IsStandalone = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RuntimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    ImdbRating = table.Column<double>(type: "double precision", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Episodes_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EpisodePlatforms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EpisodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatformId = table.Column<Guid>(type: "uuid", nullable: false),
                    WebUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AppSchemeUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodePlatforms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodePlatforms_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodePlatforms_Platforms_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "Platforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EpisodeTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EpisodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Overview = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodeTranslations_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Media_ImdbId",
                table: "Media",
                column: "ImdbId",
                unique: true,
                filter: "\"IsDeleted\" = false AND \"ImdbId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Media_ReleaseYear",
                table: "Media",
                column: "ReleaseYear");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodePlatforms_EpisodeId_PlatformId",
                table: "EpisodePlatforms",
                columns: new[] { "EpisodeId", "PlatformId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodePlatforms_IsDeleted",
                table: "EpisodePlatforms",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodePlatforms_PlatformId",
                table: "EpisodePlatforms",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_IsDeleted",
                table: "Episodes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_IsStandalone",
                table: "Episodes",
                column: "IsStandalone",
                filter: "\"IsStandalone\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_SeasonId_EpisodeNumber",
                table: "Episodes",
                columns: new[] { "SeasonId", "EpisodeNumber" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeTranslations_EpisodeId_LanguageCode",
                table: "EpisodeTranslations",
                columns: new[] { "EpisodeId", "LanguageCode" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeTranslations_IsDeleted",
                table: "EpisodeTranslations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MediaMoods_IsDeleted",
                table: "MediaMoods",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MediaMoods_MediaId_MoodId",
                table: "MediaMoods",
                columns: new[] { "MediaId", "MoodId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MediaMoods_MoodId",
                table: "MediaMoods",
                column: "MoodId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaPlatforms_IsDeleted",
                table: "MediaPlatforms",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MediaPlatforms_MediaId_PlatformId",
                table: "MediaPlatforms",
                columns: new[] { "MediaId", "PlatformId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MediaPlatforms_PlatformId",
                table: "MediaPlatforms",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaTranslations_IsDeleted",
                table: "MediaTranslations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MediaTranslations_LanguageCode",
                table: "MediaTranslations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_MediaTranslations_MediaId_LanguageCode",
                table: "MediaTranslations",
                columns: new[] { "MediaId", "LanguageCode" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MediaTranslations_Title",
                table: "MediaTranslations",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Moods_Code",
                table: "Moods",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Moods_DisplayOrder",
                table: "Moods",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Moods_IsActive",
                table: "Moods",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Moods_IsDeleted",
                table: "Moods",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MoodTranslations_IsDeleted",
                table: "MoodTranslations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MoodTranslations_MoodId_LanguageCode",
                table: "MoodTranslations",
                columns: new[] { "MoodId", "LanguageCode" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_DisplayOrder",
                table: "Platforms",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_IsActive",
                table: "Platforms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_IsDeleted",
                table: "Platforms",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_Slug",
                table: "Platforms",
                column: "Slug",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_IsDeleted",
                table: "Seasons",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_SeriesId_SeasonNumber",
                table: "Seasons",
                columns: new[] { "SeriesId", "SeasonNumber" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EpisodePlatforms");

            migrationBuilder.DropTable(
                name: "EpisodeTranslations");

            migrationBuilder.DropTable(
                name: "MediaMoods");

            migrationBuilder.DropTable(
                name: "MediaPlatforms");

            migrationBuilder.DropTable(
                name: "MediaTranslations");

            migrationBuilder.DropTable(
                name: "MoodTranslations");

            migrationBuilder.DropTable(
                name: "Episodes");

            migrationBuilder.DropTable(
                name: "Platforms");

            migrationBuilder.DropTable(
                name: "Moods");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Media",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_ImdbId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_ReleaseYear",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "BackdropUrl",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "Director",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "ImdbId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "ImdbRating",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "ImdbVotes",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "PosterUrl",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "ReleaseYear",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "RuntimeMinutes",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "SeriesType",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "TotalSeasons",
                table: "Media");

            migrationBuilder.RenameTable(
                name: "Media",
                newName: "Series");

            migrationBuilder.RenameIndex(
                name: "IX_Media_IsDeleted",
                table: "Series",
                newName: "IX_Series_IsDeleted");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Series",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsIndependentEpisodes",
                table: "Series",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Series",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Series",
                table: "Series",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Series_Title",
                table: "Series",
                column: "Title",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
