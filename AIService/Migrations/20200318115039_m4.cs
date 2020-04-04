using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AIService.Migrations
{
    public partial class m4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradeHistories",
                columns: table => new
                {
                    StockAccountId = table.Column<long>(nullable: true),
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    StockName = table.Column<string>(nullable: true),
                    StockCode = table.Column<string>(nullable: true),
                    TransactionValue = table.Column<double>(nullable: false),
                    TransactionType = table.Column<int>(nullable: false),
                    TransactionPrice = table.Column<double>(nullable: false),
                    TransactionAmount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeHistories_StockAccounts_StockAccountId",
                        column: x => x.StockAccountId,
                        principalTable: "StockAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradeHistories_StockAccountId",
                table: "TradeHistories",
                column: "StockAccountId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeHistories");
        }
    }
}
