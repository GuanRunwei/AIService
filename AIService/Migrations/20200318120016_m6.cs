using Microsoft.EntityFrameworkCore.Migrations;

namespace AIService.Migrations
{
    public partial class m6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TradeHistories_StockAccounts_StockAccountId",
                table: "TradeHistories");

            migrationBuilder.AlterColumn<long>(
                name: "StockAccountId",
                table: "TradeHistories",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TradeHistories_StockAccounts_StockAccountId",
                table: "TradeHistories",
                column: "StockAccountId",
                principalTable: "StockAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TradeHistories_StockAccounts_StockAccountId",
                table: "TradeHistories");

            migrationBuilder.AlterColumn<long>(
                name: "StockAccountId",
                table: "TradeHistories",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddForeignKey(
                name: "FK_TradeHistories_StockAccounts_StockAccountId",
                table: "TradeHistories",
                column: "StockAccountId",
                principalTable: "StockAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
