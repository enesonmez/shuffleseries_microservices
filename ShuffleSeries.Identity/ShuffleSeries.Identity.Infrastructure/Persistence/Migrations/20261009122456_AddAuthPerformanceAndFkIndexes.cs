using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuffleSeries.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthPerformanceAndFkIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_IsDeleted",
                table: "Users",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status",
                table: "Users",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAtUtc",
                table: "RefreshTokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplacedByTokenHash",
                table: "RefreshTokens",
                column: "ReplacedByTokenHash",
                filter: "\"ReplacedByTokenHash\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IsDeleted",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Status",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ExpiresAtUtc",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ReplacedByTokenHash",
                table: "RefreshTokens");
        }
    }
}
