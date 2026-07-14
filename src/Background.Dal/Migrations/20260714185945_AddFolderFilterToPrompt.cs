using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Background.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderFilterToPrompt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FolderFilter",
                table: "Prompts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_FolderFilter_IsActive",
                table: "Prompts",
                columns: new[] { "FolderFilter", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prompts_FolderFilter_IsActive",
                table: "Prompts");

            migrationBuilder.DropColumn(
                name: "FolderFilter",
                table: "Prompts");
        }
    }
}
