using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventNestBE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRegistrationForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Registrations_StudentId",
                table: "Registrations",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_Users_StudentId",
                table: "Registrations",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_Users_StudentId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_StudentId",
                table: "Registrations");
        }
    }
}
