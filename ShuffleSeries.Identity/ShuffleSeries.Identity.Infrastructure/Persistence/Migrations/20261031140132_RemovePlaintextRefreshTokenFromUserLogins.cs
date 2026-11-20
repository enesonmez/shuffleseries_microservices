using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuffleSeries.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlaintextRefreshTokenFromUserLogins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "UserLogins");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "UserLogins",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }
    }
}
