using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Challenges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentJson",
                table: "Challenges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Challenges",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "ContentJson",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Challenges");
        }
    }
}
