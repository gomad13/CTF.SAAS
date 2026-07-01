using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMaxMembersAndChallengeVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxMembers",
                table: "Teams",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantsJson",
                table: "Challenges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "ChallengeCompletions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxMembers",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "VariantsJson",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "ChallengeCompletions");
        }
    }
}
