using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventNestBE.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationTimeAndPointsToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationEndTime",
                table: "Events",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationStartTime",
                table: "Events",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TrainingPoints",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegistrationEndTime",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegistrationStartTime",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TrainingPoints",
                table: "Events");
        }
    }
}
