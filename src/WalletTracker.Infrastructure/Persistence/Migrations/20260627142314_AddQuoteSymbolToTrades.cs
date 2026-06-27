using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteSymbolToTrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TokenPositions_WalletId_TokenAddress",
                table: "TokenPositions");

            migrationBuilder.AddColumn<string>(
                name: "QuoteSymbol",
                table: "Trades",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QuoteSymbol",
                table: "TokenPositions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "QuotePnLs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletId = table.Column<int>(type: "int", nullable: false),
                    QuoteSymbol = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RealizedPnL = table.Column<decimal>(type: "decimal(38,18)", nullable: false),
                    UnrealizedPnL = table.Column<decimal>(type: "decimal(38,18)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotePnLs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotePnLs_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenPositions_WalletId_TokenAddress_QuoteSymbol",
                table: "TokenPositions",
                columns: new[] { "WalletId", "TokenAddress", "QuoteSymbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuotePnLs_WalletId_QuoteSymbol",
                table: "QuotePnLs",
                columns: new[] { "WalletId", "QuoteSymbol" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuotePnLs");

            migrationBuilder.DropIndex(
                name: "IX_TokenPositions_WalletId_TokenAddress_QuoteSymbol",
                table: "TokenPositions");

            migrationBuilder.DropColumn(
                name: "QuoteSymbol",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "QuoteSymbol",
                table: "TokenPositions");

            migrationBuilder.CreateIndex(
                name: "IX_TokenPositions_WalletId_TokenAddress",
                table: "TokenPositions",
                columns: new[] { "WalletId", "TokenAddress" },
                unique: true);
        }
    }
}
