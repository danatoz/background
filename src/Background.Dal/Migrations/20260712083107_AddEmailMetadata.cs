using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Background.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SenderAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Folder = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BodyIsHtml = table.Column<bool>(type: "boolean", nullable: true),
                    BodyS3Key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttachmentsJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailMetadata_ProcessingJobs_Id",
                        column: x => x.Id,
                        principalTable: "ProcessingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailMetadata");
        }
    }
}
