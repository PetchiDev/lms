using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CertificateTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CertificateTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrganizationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tagline = table.Column<string>(type: "text", nullable: false),
                    AwardedToLabel = table.Column<string>(type: "text", nullable: false),
                    BodyText = table.Column<string>(type: "text", nullable: false),
                    DatePrefix = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    FooterLocation = table.Column<string>(type: "text", nullable: false),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: false),
                    LeftSignatoryTitle = table.Column<string>(type: "text", nullable: false),
                    RightSignatoryTitle = table.Column<string>(type: "text", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    LeftSignatureImageUrl = table.Column<string>(type: "text", nullable: true),
                    RightSignatureImageUrl = table.Column<string>(type: "text", nullable: true),
                    PrimaryColor = table.Column<string>(type: "text", nullable: false),
                    AccentColor = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateTemplates", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateTemplates");
        }
    }
}
