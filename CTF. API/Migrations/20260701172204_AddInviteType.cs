using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "TenantInvites",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "InviteType",
                table: "TenantInvites",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "enterprise_join");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InviteType",
                table: "TenantInvites");

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "TenantInvites",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
