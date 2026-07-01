using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMemberships", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_TeamId_UserId",
                table: "TeamMemberships",
                columns: new[] { "TeamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_TenantId_TeamId",
                table: "TeamMemberships",
                columns: new[] { "TenantId", "TeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_TenantId_UserId",
                table: "TeamMemberships",
                columns: new[] { "TenantId", "UserId" });

            // ── Backfill : reprend l'appartenance mono-équipe existante (Users.TeamId)
            //    UNIQUEMENT lorsque le tenant de l'équipe correspond à celui de l'user
            //    (isolation). gen_random_uuid() requiert pgcrypto (présent sur PG 18). ──
            migrationBuilder.Sql(@"
                INSERT INTO ""TeamMemberships"" (""Id"", ""TenantId"", ""TeamId"", ""UserId"", ""JoinedAt"")
                SELECT gen_random_uuid(), u.""TenantId"", u.""TeamId"", u.""Id"", now()
                FROM ""Users"" u
                JOIN ""Teams"" t ON t.""Id"" = u.""TeamId""
                WHERE u.""TeamId"" IS NOT NULL
                  AND t.""TenantId"" = u.""TenantId""
                  AND NOT EXISTS (
                      SELECT 1 FROM ""TeamMemberships"" m
                      WHERE m.""TeamId"" = u.""TeamId"" AND m.""UserId"" = u.""Id"");
            ");

            // ── Réparation d'incohérence (point 8) : si l'équipe principale d'un user
            //    pointe vers une équipe d'un AUTRE tenant, on remet TeamId à NULL. ──
            migrationBuilder.Sql(@"
                UPDATE ""Users"" u SET ""TeamId"" = NULL
                FROM ""Teams"" t
                WHERE u.""TeamId"" = t.""Id"" AND t.""TenantId"" <> u.""TenantId"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamMemberships");
        }
    }
}
