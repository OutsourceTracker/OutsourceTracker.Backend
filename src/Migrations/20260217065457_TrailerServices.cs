using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutsourceTracker.Migrations
{
    /// <inheritdoc />
    public partial class TrailerServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trailers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Prefix = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Location = table.Column<byte[]>(type: "varbinary(24)", nullable: true),
                    LocatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LocatedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocatedDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trailers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trailers_BusinessAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "BusinessAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trailers_AccountId",
                table: "Trailers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Trailers_FullName",
                table: "Trailers",
                column: "FullName",
                unique: true,
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trailers");

            migrationBuilder.DropTable(
                name: "BusinessAccounts");
        }
    }
}
