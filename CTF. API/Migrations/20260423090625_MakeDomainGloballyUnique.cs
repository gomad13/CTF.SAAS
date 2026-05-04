using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeDomainGloballyUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantEmailDomains_Domain",
                table: "TenantEmailDomains");

            migrationBuilder.CreateIndex(
                name: "IX_TenantEmailDomains_Domain",
                table: "TenantEmailDomains",
                column: "Domain",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantEmailDomains_Domain",
                table: "TenantEmailDomains");

            migrationBuilder.CreateIndex(
                name: "IX_TenantEmailDomains_Domain",
                table: "TenantEmailDomains",
                column: "Domain");
        }
    }
}
