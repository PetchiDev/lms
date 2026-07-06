using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StudentSemesterProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentSemester",
                table: "StudentEnrolments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentYear",
                table: "StudentEnrolments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentSemester",
                table: "StudentEnrolments");

            migrationBuilder.DropColumn(
                name: "CurrentYear",
                table: "StudentEnrolments");
        }
    }
}
