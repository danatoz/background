using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Background.Dal.Migrations
{
    /// <inheritdoc />
    public partial class RenameMessagesToProcessingJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Prompts_PromptId",
                table: "Messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Payload",
                table: "Messages");

            migrationBuilder.RenameTable(
                name: "Messages",
                newName: "ProcessingJobs");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_WorkerId",
                table: "ProcessingJobs",
                newName: "IX_ProcessingJobs_WorkerId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_Status",
                table: "ProcessingJobs",
                newName: "IX_ProcessingJobs_Status");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_PromptId",
                table: "ProcessingJobs",
                newName: "IX_ProcessingJobs_PromptId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_NextRetryAt",
                table: "ProcessingJobs",
                newName: "IX_ProcessingJobs_NextRetryAt");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_LockedUntil",
                table: "ProcessingJobs",
                newName: "IX_ProcessingJobs_LockedUntil");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessingJobs_Prompts_PromptId",
                table: "ProcessingJobs",
                column: "PromptId",
                principalTable: "Prompts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessingJobs_Prompts_PromptId",
                table: "ProcessingJobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs");

            migrationBuilder.RenameTable(
                name: "ProcessingJobs",
                newName: "Messages");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessingJobs_WorkerId",
                table: "Messages",
                newName: "IX_Messages_WorkerId");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessingJobs_Status",
                table: "Messages",
                newName: "IX_Messages_Status");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessingJobs_PromptId",
                table: "Messages",
                newName: "IX_Messages_PromptId");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessingJobs_NextRetryAt",
                table: "Messages",
                newName: "IX_Messages_NextRetryAt");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessingJobs_LockedUntil",
                table: "Messages",
                newName: "IX_Messages_LockedUntil");

            migrationBuilder.AddColumn<string>(
                name: "Payload",
                table: "Messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                table: "Messages",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Prompts_PromptId",
                table: "Messages",
                column: "PromptId",
                principalTable: "Prompts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
