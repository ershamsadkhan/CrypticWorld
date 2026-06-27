using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackfillStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BackfillStatus",
                table: "Wallets",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackfillStatus",
                table: "Wallets");
        }
    }
}
