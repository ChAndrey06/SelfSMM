using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PL.Telegram.Bot.Migrations
{
    /// <inheritdoc />
    public partial class Userlanguagefield : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayedAt",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "Language",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "Users");

            migrationBuilder.AddColumn<DateTime>(
                name: "PayedAt",
                table: "Users",
                type: "timestamp without time zone",
                nullable: true);
        }
    }
}
