using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Background.Dal.Migrations
{
    /// <inheritdoc />
    public partial class ChangeResponseSchemaToJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Prompts"
                SET "ResponseSchema" = NULL
                WHERE "ResponseSchema" IS NOT NULL
                  AND "ResponseSchema"::jsonb IS NULL;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Prompts"
                ALTER COLUMN "ResponseSchema" TYPE jsonb
                USING "ResponseSchema"::jsonb;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ResponseSchema",
                table: "Prompts",
                type: "text",
                nullable: true,
                oldClrType: typeof(JsonNode),
                oldType: "jsonb",
                oldNullable: true);
        }
    }
}
