using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutsourceTracker.Migrations
{
    /// <inheritdoc />
    public partial class ZoneUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Zones",
                newName: "ShortCode");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedOn",
                table: "Zones",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "DockPoints",
                table: "Zones",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntryPoints",
                table: "Zones",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExitPoints",
                table: "Zones",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Zones",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "DockPoints",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "EntryPoints",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "ExitPoints",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Zones");

            migrationBuilder.RenameColumn(
                name: "ShortCode",
                table: "Zones",
                newName: "Name");
        }
    }
}
