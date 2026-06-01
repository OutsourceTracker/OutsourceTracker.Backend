using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutsourceTracker.OutsourceTracker.Backend.src.Migrations
{
    /// <inheritdoc />
    public partial class AddTrailerPoolsToZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrailerPools",
                table: "Zones",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrailerPools",
                table: "Zones");
        }
    }
}
