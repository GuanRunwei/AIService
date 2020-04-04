using Microsoft.EntityFrameworkCore.Migrations;

namespace AIService.Migrations
{
    public partial class m3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Profit_and_Loss",
                table: "StockAccounts",
                newName: "Profit_or_Loss");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Profit_or_Loss",
                table: "StockAccounts",
                newName: "Profit_and_Loss");
        }
    }
}
