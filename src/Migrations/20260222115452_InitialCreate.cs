using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutsourceTracker.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                name: "Zones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Boundry = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.Id);
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
                    Location = table.Column<byte[]>(type: "BLOB", nullable: true),
                    LocationAccuracy = table.Column<double>(type: "REAL", nullable: true),
                    LocatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LocatedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocatedDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ZoneId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ZoneName = table.Column<string>(type: "TEXT", nullable: true)
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
                name: "Zones");

            migrationBuilder.DropTable(
                name: "BusinessAccounts");
        }
    }
}
