using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutsourceTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddTrailerAccountDisplayProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                table: "Trailers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountShortCode",
                table: "Trailers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountName",
                table: "Trailers");

            migrationBuilder.DropColumn(
                name: "AccountShortCode",
                table: "Trailers");
        }
    }
}
