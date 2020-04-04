using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AIService.Migrations
{
    public partial class m2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SellStocks",
                columns: table => new
                {
                    StockAccountId = table.Column<long>(nullable: false),
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    StockCode = table.Column<string>(nullable: true),
                    StockName = table.Column<string>(nullable: true),
                    BuyPrice = table.Column<double>(nullable: false),
                    SellPrice = table.Column<double>(nullable: false),
                    SellStockNumber = table.Column<int>(nullable: false),
                    SellTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellStocks_StockAccounts_StockAccountId",
                        column: x => x.StockAccountId,
                        principalTable: "StockAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellStocks_StockAccountId",
                table: "SellStocks",
                column: "StockAccountId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellStocks");
        }
    }
}
