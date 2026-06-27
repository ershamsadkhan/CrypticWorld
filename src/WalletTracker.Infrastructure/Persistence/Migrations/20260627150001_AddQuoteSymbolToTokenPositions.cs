using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteSymbolToTokenPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old unique index first
            migrationBuilder.DropIndex(
                name: "IX_TokenPositions_WalletId_TokenAddress",
                table: "TokenPositions");

            // Add QuoteSymbol column as nullable first
            migrationBuilder.AddColumn<string>(
                name: "QuoteSymbol",
                table: "TokenPositions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            // Update all existing records to have a default QuoteSymbol value
            migrationBuilder.Sql("UPDATE TokenPositions SET QuoteSymbol = 'UNKNOWN' WHERE QuoteSymbol IS NULL");

            // Make QuoteSymbol not nullable
            migrationBuilder.AlterColumn<string>(
                name: "QuoteSymbol",
                table: "TokenPositions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            // Create new unique index with QuoteSymbol
            migrationBuilder.CreateIndex(
                name: "IX_TokenPositions_WalletId_TokenAddress_QuoteSymbol",
                table: "TokenPositions",
                columns: new[] { "WalletId", "TokenAddress", "QuoteSymbol" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the new unique index
            migrationBuilder.DropIndex(
                name: "IX_TokenPositions_WalletId_TokenAddress_QuoteSymbol",
                table: "TokenPositions");

            // Remove QuoteSymbol column
            migrationBuilder.DropColumn(
                name: "QuoteSymbol",
                table: "TokenPositions");

            // Recreate old unique index
            migrationBuilder.CreateIndex(
                name: "IX_TokenPositions_WalletId_TokenAddress",
                table: "TokenPositions",
                columns: new[] { "WalletId", "TokenAddress" },
                unique: true);
        }
    }
}
