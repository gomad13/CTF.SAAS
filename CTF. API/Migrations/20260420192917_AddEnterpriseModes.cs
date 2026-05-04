using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AnalyticsUpdatedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AnalyticsUpdatedBy",
                table: "Tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CampaignsUpdatedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CampaignsUpdatedBy",
                table: "Tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ComplianceUpdatedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ComplianceUpdatedBy",
                table: "Tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnalyticsEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCampaignsEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsComplianceEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTeamsEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TeamsUpdatedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeamsUpdatedBy",
                table: "Tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Teams",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Teams",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Teams",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CampaignParticipations",
                columns: table => new
                {
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedParcoursCount = table.Column<int>(type: "integer", nullable: false),
                    TotalScore = table.Column<int>(type: "integer", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignParticipations", x => new { x.CampaignId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "CampaignPaths",
                columns: table => new
                {
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignPaths", x => new { x.CampaignId, x.PathId });
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CampaignTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignTargets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MandatoryAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedToType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uuid", nullable: true),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandatoryAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Link = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TenantId_Name",
                table: "Teams",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignParticipations_TenantId_CampaignId",
                table: "CampaignParticipations",
                columns: new[] { "TenantId", "CampaignId" });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_StartDate",
                table: "Campaigns",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_TenantId_Status",
                table: "Campaigns",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTargets_CampaignId",
                table: "CampaignTargets",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_MandatoryAssignments_Deadline",
                table: "MandatoryAssignments",
                column: "Deadline");

            migrationBuilder.CreateIndex(
                name: "IX_MandatoryAssignments_TenantId_PathId",
                table: "MandatoryAssignments",
                columns: new[] { "TenantId", "PathId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "TenantId", "UserId", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignParticipations");

            migrationBuilder.DropTable(
                name: "CampaignPaths");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "CampaignTargets");

            migrationBuilder.DropTable(
                name: "MandatoryAssignments");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Teams_TenantId_Name",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "AnalyticsUpdatedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AnalyticsUpdatedBy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CampaignsUpdatedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CampaignsUpdatedBy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ComplianceUpdatedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ComplianceUpdatedBy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsAnalyticsEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsCampaignsEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsComplianceEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsTeamsEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TeamsUpdatedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TeamsUpdatedBy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Teams");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Teams",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);
        }
    }
}
