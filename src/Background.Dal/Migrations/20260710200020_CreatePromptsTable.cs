using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Background.Dal.Migrations
{
    /// <inheritdoc />
    public partial class CreatePromptsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "Messages");

            migrationBuilder.AddColumn<Guid>(
                name: "PromptId",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Prompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prompts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_PromptId",
                table: "Messages",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_Name_Version",
                table: "Prompts",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Prompts_PromptId",
                table: "Messages",
                column: "PromptId",
                principalTable: "Prompts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Prompts_PromptId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "Prompts");

            migrationBuilder.DropIndex(
                name: "IX_Messages_PromptId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PromptId",
                table: "Messages");

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "Messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                table: "Messages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
