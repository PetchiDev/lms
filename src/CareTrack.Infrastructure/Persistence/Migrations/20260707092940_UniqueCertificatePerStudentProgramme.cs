using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueCertificatePerStudentProgramme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cleanup: keep only the latest certificate per (StudentId, ProgrammeId)
            migrationBuilder.Sql(@"
DELETE FROM ""Certificates"" c
USING ""Certificates"" d
WHERE c.""StudentId"" = d.""StudentId""
  AND c.""ProgrammeId"" = d.""ProgrammeId""
  AND (
       c.""IssuedAt"" < d.""IssuedAt""
       OR (c.""IssuedAt"" = d.""IssuedAt"" AND c.""Id"" < d.""Id"")
  );
");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_StudentId",
                table: "Certificates");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_StudentId_ProgrammeId",
                table: "Certificates",
                columns: new[] { "StudentId", "ProgrammeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Certificates_StudentId_ProgrammeId",
                table: "Certificates");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_StudentId",
                table: "Certificates",
                column: "StudentId");
        }
    }
}
