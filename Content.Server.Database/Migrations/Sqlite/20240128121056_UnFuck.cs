using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class UnFuck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user_id",
                table: "server_role_ban");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "server_ban");

            migrationBuilder.DropColumn(
                name: "body_type",
                table: "profile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "server_role_ban",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "server_ban",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "body_type",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
