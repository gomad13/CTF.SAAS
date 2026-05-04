using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogParcoursAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinutes",
                table: "Paths",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCatalog",
                table: "Paths",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "Paths",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Paths",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantParcoursAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantParcoursAccesses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Paths_IsCatalog_Sector",
                table: "Paths",
                columns: new[] { "IsCatalog", "Sector" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantParcoursAccesses_PathId_RevokedAt",
                table: "TenantParcoursAccesses",
                columns: new[] { "PathId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantParcoursAccesses_TenantId_PathId",
                table: "TenantParcoursAccesses",
                columns: new[] { "TenantId", "PathId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantParcoursAccesses_TenantId_RevokedAt",
                table: "TenantParcoursAccesses",
                columns: new[] { "TenantId", "RevokedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantParcoursAccesses");

            migrationBuilder.DropIndex(
                name: "IX_Paths_IsCatalog_Sector",
                table: "Paths");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "Paths");

            migrationBuilder.DropColumn(
                name: "IsCatalog",
                table: "Paths");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "Paths");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Paths");
        }
    }
}
