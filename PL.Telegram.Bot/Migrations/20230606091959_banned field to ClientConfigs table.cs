using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PL.Telegram.Bot.Migrations
{
    /// <inheritdoc />
    public partial class bannedfieldtoClientConfigstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Banned",
                table: "ClientConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Banned",
                table: "ClientConfigs");
        }
    }
}
