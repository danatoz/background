using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Background.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddPipelineArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArtifactPrefix",
                table: "Messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentStep",
                table: "Messages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "Messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipelineVersion",
                table: "Messages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                table: "Messages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArtifactPrefix",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "CurrentStep",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PipelineVersion",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "Messages");
        }
    }
}
