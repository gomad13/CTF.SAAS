using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "TenantEmailDomains",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckedAt",
                table: "TenantEmailDomains",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationToken",
                table: "TenantEmailDomains",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "TenantEmailDomains",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedBy",
                table: "TenantEmailDomains",
                type: "uuid",
                nullable: true);

            // Grandfather : les domaines déjà déclarés (via SuperAdmin) sont établis manuellement
            // par la plateforme → considérés vérifiés, pour ne casser aucune hypothèse existante.
            migrationBuilder.Sql(
                "UPDATE \"TenantEmailDomains\" SET \"IsVerified\" = true, \"VerifiedAt\" = \"CreatedAt\" WHERE \"IsVerified\" = false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "TenantEmailDomains");

            migrationBuilder.DropColumn(
                name: "LastCheckedAt",
                table: "TenantEmailDomains");

            migrationBuilder.DropColumn(
                name: "VerificationToken",
                table: "TenantEmailDomains");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "TenantEmailDomains");

            migrationBuilder.DropColumn(
                name: "VerifiedBy",
                table: "TenantEmailDomains");
        }
    }
}
