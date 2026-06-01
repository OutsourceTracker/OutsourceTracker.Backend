using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutsourceTracker.Migrations
{
    /// <summary>
    /// Backfills any empty string values in the JSON point collection columns
    /// (including the newly added TrailerPools) with valid empty JSON arrays '[]'.
    /// This fixes deserialization errors caused by the DEFAULT '' from the previous migration.
    /// </summary>
    public partial class FixEmptyJsonDefaults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill TrailerPools (the column that triggered the current error)
            migrationBuilder.Sql(
                @"UPDATE ""Zones"" 
                  SET ""TrailerPools"" = '[]' 
                  WHERE ""TrailerPools"" IS NULL OR ""TrailerPools"" = '';");

            // Also clean up the older point collection columns defensively
            // (they may have the same latent issue from earlier migrations)
            migrationBuilder.Sql(
                @"UPDATE ""Zones"" 
                  SET ""EntryPoints"" = '[]' 
                  WHERE ""EntryPoints"" IS NULL OR ""EntryPoints"" = '';");

            migrationBuilder.Sql(
                @"UPDATE ""Zones"" 
                  SET ""ExitPoints"" = '[]' 
                  WHERE ""ExitPoints"" IS NULL OR ""ExitPoints"" = '';");

            migrationBuilder.Sql(
                @"UPDATE ""Zones"" 
                  SET ""DockPoints"" = '[]' 
                  WHERE ""DockPoints"" IS NULL OR ""DockPoints"" = '';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op on down — we don't want to re-introduce bad empty strings.
            // If someone really wants to revert, they can do it manually.
        }
    }
}
