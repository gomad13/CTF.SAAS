using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignContentsAndAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AssignedToWholeTenant",
                table: "Campaigns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Campaigns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Campaigns",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "CampaignAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignAssignments_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampaignContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignContents_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CompletionPercentage = table.Column<double>(type: "double precision", nullable: true),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignProgresses_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_TenantId_IsArchived",
                table: "Campaigns",
                columns: new[] { "TenantId", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_TenantId_StartDate",
                table: "Campaigns",
                columns: new[] { "TenantId", "StartDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignAssignments_CampaignId_UserId",
                table: "CampaignAssignments",
                columns: new[] { "CampaignId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignAssignments_UserId_TenantId",
                table: "CampaignAssignments",
                columns: new[] { "UserId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContents_CampaignId",
                table: "CampaignContents",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContents_TenantId_ContentType",
                table: "CampaignContents",
                columns: new[] { "TenantId", "ContentType" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignProgresses_CampaignId_CampaignContentId_UserId",
                table: "CampaignProgresses",
                columns: new[] { "CampaignId", "CampaignContentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignProgresses_CampaignId_UserId",
                table: "CampaignProgresses",
                columns: new[] { "CampaignId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignProgresses_UserId_Status",
                table: "CampaignProgresses",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignAssignments");

            migrationBuilder.DropTable(
                name: "CampaignContents");

            migrationBuilder.DropTable(
                name: "CampaignProgresses");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_TenantId_IsArchived",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_TenantId_StartDate",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "AssignedToWholeTenant",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Campaigns");
        }
    }
}
