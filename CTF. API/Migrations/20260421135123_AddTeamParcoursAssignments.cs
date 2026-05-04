using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamParcoursAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Teams",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerId",
                table: "Teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Teams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TeamParcoursAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamParcoursAssignments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamParcoursAssignments_TeamId_PathId",
                table: "TeamParcoursAssignments",
                columns: new[] { "TeamId", "PathId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamParcoursAssignments_TenantId_TeamId",
                table: "TeamParcoursAssignments",
                columns: new[] { "TenantId", "TeamId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamParcoursAssignments");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Teams");
        }
    }
}
