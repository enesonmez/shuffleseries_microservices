using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeCatalogIndexesAndHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Seasons_IsDeleted",
                table: "Seasons");

            migrationBuilder.DropIndex(
                name: "IX_MoodTranslations_IsDeleted",
                table: "MoodTranslations");

            migrationBuilder.DropIndex(
                name: "IX_Moods_DisplayOrder",
                table: "Moods");

            migrationBuilder.DropIndex(
                name: "IX_Moods_IsActive",
                table: "Moods");

            migrationBuilder.DropIndex(
                name: "IX_Moods_IsDeleted",
                table: "Moods");

            migrationBuilder.DropIndex(
                name: "IX_MediaTranslations_IsDeleted",
                table: "MediaTranslations");

            migrationBuilder.DropIndex(
                name: "IX_MediaTranslations_LanguageCode",
                table: "MediaTranslations");

            migrationBuilder.DropIndex(
                name: "IX_MediaPlatforms_IsDeleted",
                table: "MediaPlatforms");

            migrationBuilder.DropIndex(
                name: "IX_MediaMoods_IsDeleted",
                table: "MediaMoods");

            migrationBuilder.DropIndex(
                name: "IX_MediaGenres_IsDeleted",
                table: "MediaGenres");

            migrationBuilder.DropIndex(
                name: "IX_Media_IsDeleted",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_GenreTranslations_IsDeleted",
                table: "GenreTranslations");

            migrationBuilder.DropIndex(
                name: "IX_Genres_DisplayOrder",
                table: "Genres");

            migrationBuilder.DropIndex(
                name: "IX_Genres_IsActive",
                table: "Genres");

            migrationBuilder.DropIndex(
                name: "IX_Genres_IsDeleted",
                table: "Genres");

            migrationBuilder.DropIndex(
                name: "IX_EpisodeTranslations_IsDeleted",
                table: "EpisodeTranslations");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_IsDeleted",
                table: "Episodes");

            migrationBuilder.DropIndex(
                name: "IX_EpisodePlatforms_IsDeleted",
                table: "EpisodePlatforms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Seasons_IsDeleted",
                table: "Seasons",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MoodTranslations_IsDeleted",
                table: "MoodTranslations",
                column: "IsDeleted");

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
                name: "IX_MediaTranslations_IsDeleted",
                table: "MediaTranslations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MediaTranslations_LanguageCode",
                table: "MediaTranslations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_MediaPlatforms_IsDeleted",
                table: "MediaPlatforms",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MediaMoods_IsDeleted",
                table: "MediaMoods",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MediaGenres_IsDeleted",
                table: "MediaGenres",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Media_IsDeleted",
                table: "Media",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_GenreTranslations_IsDeleted",
                table: "GenreTranslations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Genres_DisplayOrder",
                table: "Genres",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Genres_IsActive",
                table: "Genres",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Genres_IsDeleted",
                table: "Genres",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeTranslations_IsDeleted",
                table: "EpisodeTranslations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_IsDeleted",
                table: "Episodes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodePlatforms_IsDeleted",
                table: "EpisodePlatforms",
                column: "IsDeleted");
        }
    }
}
