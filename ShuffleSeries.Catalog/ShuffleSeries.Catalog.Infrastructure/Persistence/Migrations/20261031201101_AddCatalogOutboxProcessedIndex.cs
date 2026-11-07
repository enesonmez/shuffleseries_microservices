using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogOutboxProcessedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Processed",
                table: "OutboxMessages",
                column: "ProcessedOnUtc",
                filter: "\"ProcessedOnUtc\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Processed",
                table: "OutboxMessages");
        }
    }
}
