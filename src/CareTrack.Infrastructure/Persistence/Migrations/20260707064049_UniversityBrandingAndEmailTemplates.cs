using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniversityBrandingAndEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailFromEmail",
                table: "Universities",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailFromName",
                table: "Universities",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailInviteBodyHtml",
                table: "Universities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailInviteSubject",
                table: "Universities",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Universities",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailFromEmail",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "EmailFromName",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "EmailInviteBodyHtml",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "EmailInviteSubject",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Universities");
        }
    }
}
