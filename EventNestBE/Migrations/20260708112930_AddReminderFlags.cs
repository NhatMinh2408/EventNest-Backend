using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventNestBE.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Is1hReminderSent",
                table: "Registrations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Is24hReminderSent",
                table: "Registrations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Is1hReminderSent",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "Is24hReminderSent",
                table: "Registrations");
        }
    }
}
