using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Chain = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastCursor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewTokenAlerts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletId = table.Column<int>(type: "int", nullable: false),
                    TokenAddress = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TokenSymbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewTokenAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewTokenAlerts_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokenPositions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletId = table.Column<int>(type: "int", nullable: false),
                    TokenAddress = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TokenSymbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuantityHeld = table.Column<decimal>(type: "decimal(38,18)", nullable: false),
                    LastKnownPriceInQuote = table.Column<decimal>(type: "decimal(38,18)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenPositions_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletId = table.Column<int>(type: "int", nullable: false),
                    Chain = table.Column<int>(type: "int", nullable: false),
                    TxHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    BlockTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    TokenAddress = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TokenSymbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AmountToken = table.Column<decimal>(type: "decimal(38,18)", nullable: false),
                    AmountQuote = table.Column<decimal>(type: "decimal(38,18)", nullable: false),
                    PricePerTokenInQuote = table.Column<decimal>(type: "decimal(38,18)", nullable: false),
                    DexName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trades_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalletStats",
                columns: table => new
                {
                    WalletId = table.Column<int>(type: "int", nullable: false),
                    TotalTrades = table.Column<int>(type: "int", nullable: false),
                    TotalSells = table.Column<int>(type: "int", nullable: false),
                    WinningSells = table.Column<int>(type: "int", nullable: false),
                    WinRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    RealizedPnLInQuote = table.Column<decimal>(type: "decimal(38,18)", nullable: false),
                    UnrealizedPnLInQuote = table.Column<decimal>(type: "decimal(38,18)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletStats", x => x.WalletId);
                    table.ForeignKey(
                        name: "FK_WalletStats_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostBasisLots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TokenPositionId = table.Column<long>(type: "bigint", nullable: false),
                    QuantityRemaining = table.Column<decimal>(type: "decimal(38,18)", nullable: false),
                    PricePerTokenInQuote = table.Column<decimal>(type: "decimal(38,18)", nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceTradeId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostBasisLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostBasisLots_TokenPositions_TokenPositionId",
                        column: x => x.TokenPositionId,
                        principalTable: "TokenPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostBasisLots_TokenPositionId",
                table: "CostBasisLots",
                column: "TokenPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_NewTokenAlerts_WalletId_TokenAddress",
                table: "NewTokenAlerts",
                columns: new[] { "WalletId", "TokenAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenPositions_WalletId_TokenAddress",
                table: "TokenPositions",
                columns: new[] { "WalletId", "TokenAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trades_WalletId_TokenAddress",
                table: "Trades",
                columns: new[] { "WalletId", "TokenAddress" });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_WalletId_TxHash",
                table: "Trades",
                columns: new[] { "WalletId", "TxHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_Address_Chain",
                table: "Wallets",
                columns: new[] { "Address", "Chain" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostBasisLots");

            migrationBuilder.DropTable(
                name: "NewTokenAlerts");

            migrationBuilder.DropTable(
                name: "NotificationChannels");

            migrationBuilder.DropTable(
                name: "Trades");

            migrationBuilder.DropTable(
                name: "WalletStats");

            migrationBuilder.DropTable(
                name: "TokenPositions");

            migrationBuilder.DropTable(
                name: "Wallets");
        }
    }
}
