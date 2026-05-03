using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Srm.Gateway.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalationLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "escalation_level",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "escalation_level",
                table: "documents");
        }
    }
}
