using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenantCertificateTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UniversityId",
                table: "CertificateTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateTemplates_UniversityId",
                table: "CertificateTemplates",
                column: "UniversityId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateTemplates_Universities_UniversityId",
                table: "CertificateTemplates",
                column: "UniversityId",
                principalTable: "Universities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateTemplates_Universities_UniversityId",
                table: "CertificateTemplates");

            migrationBuilder.DropIndex(
                name: "IX_CertificateTemplates_UniversityId",
                table: "CertificateTemplates");

            migrationBuilder.DropColumn(
                name: "UniversityId",
                table: "CertificateTemplates");
        }
    }
}
