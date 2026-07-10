using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Background.Dal.Migrations
{
    /// <inheritdoc />
    public partial class RenameCurrentStepToLastStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CurrentStep",
                table: "Messages",
                newName: "LastStep");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastStep",
                table: "Messages",
                newName: "CurrentStep");
        }
    }
}
