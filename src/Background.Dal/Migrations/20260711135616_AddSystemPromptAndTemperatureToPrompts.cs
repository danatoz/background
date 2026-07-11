using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Background.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemPromptAndTemperatureToPrompts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SystemPrompt",
                table: "Prompts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Temperature",
                table: "Prompts",
                type: "double precision",
                precision: 3,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SystemPrompt",
                table: "Prompts");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "Prompts");
        }
    }
}
