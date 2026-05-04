using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantParcoursAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantParcoursAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "text", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeactivatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantParcoursAssignments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantParcoursAssignments_TenantId_DeactivatedAt",
                table: "TenantParcoursAssignments",
                columns: new[] { "TenantId", "DeactivatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantParcoursAssignments_TenantId_PathId",
                table: "TenantParcoursAssignments",
                columns: new[] { "TenantId", "PathId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantParcoursAssignments");
        }
    }
}
