using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutsourceTracker.Migrations
{
    /// <inheritdoc />
    public partial class EnhancePasskeyMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUsedOn",
                table: "PasskeyMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicKey",
                table: "PasskeyMetadata",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<uint>(
                name: "SignCount",
                table: "PasskeyMetadata",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "Transports",
                table: "PasskeyMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "PasskeyMetadata",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyMetadata_UserId",
                table: "PasskeyMetadata",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PasskeyMetadata_AspNetUsers_UserId",
                table: "PasskeyMetadata",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PasskeyMetadata_AspNetUsers_UserId",
                table: "PasskeyMetadata");

            migrationBuilder.DropIndex(
                name: "IX_PasskeyMetadata_UserId",
                table: "PasskeyMetadata");

            migrationBuilder.DropColumn(
                name: "LastUsedOn",
                table: "PasskeyMetadata");

            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "PasskeyMetadata");

            migrationBuilder.DropColumn(
                name: "SignCount",
                table: "PasskeyMetadata");

            migrationBuilder.DropColumn(
                name: "Transports",
                table: "PasskeyMetadata");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PasskeyMetadata");
        }
    }
}
