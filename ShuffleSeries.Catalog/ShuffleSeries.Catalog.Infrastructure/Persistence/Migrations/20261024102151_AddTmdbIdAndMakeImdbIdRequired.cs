using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTmdbIdAndMakeImdbIdRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Media_ImdbId",
                table: "Media");

            migrationBuilder.AlterColumn<string>(
                name: "ImdbId",
                table: "Media",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TmdbId",
                table: "Media",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"Media\" SET \"TmdbId\" = (100000 + abs(hashtext(\"Id\"::text)) % 800000)::int WHERE \"TmdbId\" = 0;");

            migrationBuilder.CreateIndex(
                name: "IX_Media_ImdbId",
                table: "Media",
                column: "ImdbId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Media_TmdbId",
                table: "Media",
                column: "TmdbId",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Media_ImdbId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_TmdbId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "TmdbId",
                table: "Media");

            migrationBuilder.AlterColumn<string>(
                name: "ImdbId",
                table: "Media",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_Media_ImdbId",
                table: "Media",
                column: "ImdbId",
                unique: true,
                filter: "\"IsDeleted\" = false AND \"ImdbId\" IS NOT NULL");
        }
    }
}
