using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompetitionModeUpdatedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompetitionModeUpdatedBy",
                table: "Tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompetitionModeEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompetitionModeUpdatedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CompetitionModeUpdatedBy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsCompetitionModeEnabled",
                table: "Tenants");
        }
    }
}
