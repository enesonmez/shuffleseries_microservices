using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuffleSeries.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenIdentitySoftDeleteIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IsDeleted",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserLogins_Provider_ProviderKey",
                table: "UserLogins");

            migrationBuilder.DropIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Code",
                table: "Permissions");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_Provider_ProviderKey",
                table: "UserLogins",
                columns: new[] { "Provider", "ProviderKey" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserLogins_Provider_ProviderKey",
                table: "UserLogins");

            migrationBuilder.DropIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Code",
                table: "Permissions");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsDeleted",
                table: "Users",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_Provider_ProviderKey",
                table: "UserLogins",
                columns: new[] { "Provider", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);
        }
    }
}
