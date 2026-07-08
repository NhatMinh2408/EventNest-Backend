using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventNestBE.Migrations
{
    /// <inheritdoc />
    public partial class AddPenaltyPointsColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PenaltyPoints",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PenaltyPoints",
                table: "Events");
        }
    }
}
