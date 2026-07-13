using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Background.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddExtraLlmParams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxTokens",
                table: "Prompts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseFormat",
                table: "Prompts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Seed",
                table: "Prompts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TopP",
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
                name: "MaxTokens",
                table: "Prompts");

            migrationBuilder.DropColumn(
                name: "ResponseFormat",
                table: "Prompts");

            migrationBuilder.DropColumn(
                name: "Seed",
                table: "Prompts");

            migrationBuilder.DropColumn(
                name: "TopP",
                table: "Prompts");
        }
    }
}
