using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DefaultTeamsOpen",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Tenants",
                type: "text",
                nullable: true);

            // Défaut true : les tenants existants gardent le SSO autorisé (cohérent avec le modèle).
            migrationBuilder.AddColumn<bool>(
                name: "GoogleSsoEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "MicrosoftSsoEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "Tenants",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultTeamsOpen",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "GoogleSsoEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MicrosoftSsoEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "Tenants");
        }
    }
}
