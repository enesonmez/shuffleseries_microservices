using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogGenreSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GenreTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GenreId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_GenreTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenreTranslations_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaGenres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenreId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_MediaGenres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MediaGenres_Media_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Code",
                table: "Genres",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

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
                name: "IX_GenreTranslations_GenreId_LanguageCode",
                table: "GenreTranslations",
                columns: new[] { "GenreId", "LanguageCode" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_GenreTranslations_IsDeleted",
                table: "GenreTranslations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MediaGenres_GenreId",
                table: "MediaGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaGenres_IsDeleted",
                table: "MediaGenres",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MediaGenres_MediaId_GenreId",
                table: "MediaGenres",
                columns: new[] { "MediaId", "GenreId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_IsStandalone",
                table: "Episodes");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_ImdbRating",
                table: "Episodes",
                column: "ImdbRating",
                filter: "\"IsStandalone\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Media_MediaType_CreatedAtUtc",
                table: "Media",
                columns: new[] { "MediaType", "CreatedAtUtc" },
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Media_MediaType_CreatedAtUtc",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_ImdbRating",
                table: "Episodes");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_IsStandalone",
                table: "Episodes",
                column: "IsStandalone",
                filter: "\"IsStandalone\" = true AND \"IsDeleted\" = false");

            migrationBuilder.DropTable(
                name: "GenreTranslations");

            migrationBuilder.DropTable(
                name: "MediaGenres");

            migrationBuilder.DropTable(
                name: "Genres");
        }
    }
}
