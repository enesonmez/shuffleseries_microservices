using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeTmdbIdUniquePerMediaType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Media_TmdbId",
                table: "Media");

            migrationBuilder.CreateIndex(
                name: "IX_Media_MediaType_TmdbId",
                table: "Media",
                columns: new[] { "MediaType", "TmdbId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Media_MediaType_TmdbId",
                table: "Media");

            migrationBuilder.CreateIndex(
                name: "IX_Media_TmdbId",
                table: "Media",
                column: "TmdbId",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
