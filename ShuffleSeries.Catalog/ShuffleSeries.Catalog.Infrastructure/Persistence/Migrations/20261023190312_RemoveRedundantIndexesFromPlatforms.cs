using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantIndexesFromPlatforms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Platforms_DisplayOrder",
                table: "Platforms");

            migrationBuilder.DropIndex(
                name: "IX_Platforms_IsActive",
                table: "Platforms");

            migrationBuilder.DropIndex(
                name: "IX_Platforms_IsDeleted",
                table: "Platforms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
