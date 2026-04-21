using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutsourceTracker.Migrations
{
    /// <inheritdoc />
    public partial class NewTrailerModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trailers_FullName",
                table: "Trailers");

            migrationBuilder.DropColumn(
                name: "LocatedById",
                table: "Trailers");

            migrationBuilder.CreateIndex(
                name: "IX_Trailer_FullName_Unique",
                table: "Trailers",
                column: "FullName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trailer_Name",
                table: "Trailers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Trailer_Prefix",
                table: "Trailers",
                column: "Prefix");

            migrationBuilder.CreateIndex(
                name: "IX_Trailers_ZoneId",
                table: "Trailers",
                column: "ZoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trailers_Zones_ZoneId",
                table: "Trailers",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trailers_Zones_ZoneId",
                table: "Trailers");

            migrationBuilder.DropIndex(
                name: "IX_Trailer_FullName_Unique",
                table: "Trailers");

            migrationBuilder.DropIndex(
                name: "IX_Trailer_Name",
                table: "Trailers");

            migrationBuilder.DropIndex(
                name: "IX_Trailer_Prefix",
                table: "Trailers");

            migrationBuilder.DropIndex(
                name: "IX_Trailers_ZoneId",
                table: "Trailers");

            migrationBuilder.AddColumn<Guid>(
                name: "LocatedById",
                table: "Trailers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trailers_FullName",
                table: "Trailers",
                column: "FullName",
                unique: true,
                descending: new bool[0]);
        }
    }
}
