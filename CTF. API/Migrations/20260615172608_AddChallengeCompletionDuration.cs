using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeCompletionDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "DurationSeconds",
                table: "ChallengeCompletions");
        }
    }
}
