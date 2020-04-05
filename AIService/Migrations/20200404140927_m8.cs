using Microsoft.EntityFrameworkCore.Migrations;

namespace AIService.Migrations
{
    public partial class m8 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "KnowledgeId",
                table: "SearchHistories",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_KnowledgeId",
                table: "SearchHistories",
                column: "KnowledgeId");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchHistories_Knowledges_KnowledgeId",
                table: "SearchHistories",
                column: "KnowledgeId",
                principalTable: "Knowledges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SearchHistories_Knowledges_KnowledgeId",
                table: "SearchHistories");

            migrationBuilder.DropIndex(
                name: "IX_SearchHistories_KnowledgeId",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "KnowledgeId",
                table: "SearchHistories");
        }
    }
}
