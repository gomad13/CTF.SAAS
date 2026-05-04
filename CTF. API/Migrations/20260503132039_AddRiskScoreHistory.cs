using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskScoreHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RiskScoreHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    Components = table.Column<string>(type: "jsonb", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskScoreHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiskScoreHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RiskScoreHistories_ComputedAt",
                table: "RiskScoreHistories",
                column: "ComputedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RiskScoreHistories_TenantId",
                table: "RiskScoreHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskScoreHistories_UserId",
                table: "RiskScoreHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskScoreHistories_UserId_ComputedAt",
                table: "RiskScoreHistories",
                columns: new[] { "UserId", "ComputedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiskScoreHistories");
        }
    }
}
