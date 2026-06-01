
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Authentication;

namespace OutsourceTracker.Data;

public class AppDataContextInitializer : IHostedService
{
    private IServiceProvider _services;
    private IConfiguration _configuration;

    public AppDataContextInitializer(IServiceProvider services)
    {
        _services = services;
        _configuration = _services.GetRequiredService<IConfiguration>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDataContext>();
        await db.Database.MigrateAsync(cancellationToken);

        // One-time backfill for JSON collection columns that may contain empty strings
        // from previous migrations (especially TrailerPools after AddTrailerPoolsToZone).
        // This prevents System.Text.Json deserialization errors on read.
        await BackfillEmptyJsonCollectionsAsync(db, cancellationToken);

        //if (await db.Users.CountAsync(cancellationToken) <= 0)
        //{
        //    string? adminUser = _configuration.GetValue<string>("AdminEmail");
        //    string? adminPassword = _configuration.GetValue<string>("AdminPassword");

        //    if (!string.IsNullOrWhiteSpace(adminUser) && !string.IsNullOrWhiteSpace(adminPassword))
        //    {
        //        UserManager<ApplicationUser> users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        //        ApplicationUser user = new ApplicationUser()
        //        {
        //            Email = adminUser,
        //            FirstName = "Global",
        //            LastName = "Administrator",
        //            FullName = "Global Administrator",
        //            UserName = adminUser,
        //            AlphaCode = "VANA7",
        //            WorkdayId = "400054"
        //        };

        //        var result = await users.CreateAsync(user, adminPassword);

        //        if (result.Succeeded)
        //        {
        //            await users.SetLockoutEnabledAsync(user, false);
        //            user.EmailConfirmed = true;
        //            await users.UpdateAsync(user);
        //        }
        //    }
        //}
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task BackfillEmptyJsonCollectionsAsync(AppDataContext db, CancellationToken cancellationToken)
    {
        try
        {
            // Always attempt backfill — the SQL is idempotent and very fast.
            // This fixes any rows that have empty string from the DEFAULT '' in AddTrailerPoolsToZone.
            await db.Database.ExecuteSqlRawAsync(@"
                UPDATE ""Zones"" 
                SET 
                    ""TrailerPools"" = CASE WHEN ""TrailerPools"" IS NULL OR ""TrailerPools"" = '' THEN '[]' ELSE ""TrailerPools"" END,
                    ""EntryPoints""  = CASE WHEN ""EntryPoints""  IS NULL OR ""EntryPoints""  = '' THEN '[]' ELSE ""EntryPoints""  END,
                    ""ExitPoints""   = CASE WHEN ""ExitPoints""   IS NULL OR ""ExitPoints""   = '' THEN '[]' ELSE ""ExitPoints""   END,
                    ""DockPoints""   = CASE WHEN ""DockPoints""   IS NULL OR ""DockPoints""   = '' THEN '[]' ELSE ""DockPoints""   END
                WHERE 
                    ""TrailerPools"" IS NULL OR ""TrailerPools"" = '' OR
                    ""EntryPoints""  IS NULL OR ""EntryPoints""  = '' OR
                    ""ExitPoints""   IS NULL OR ""ExitPoints""   = '' OR
                    ""DockPoints""   IS NULL OR ""DockPoints""   = '';
            ", cancellationToken);

            // Optional: log only if we actually changed something (simple heuristic)
            Console.WriteLine("Checked/backfilled empty JSON collections in Zones table (if any existed).");
        }
        catch (Exception ex)
        {
            // Don't crash startup — the defensive converters in AppDataContext will prevent deserialization crashes on read.
            Console.WriteLine($"Warning: Failed to backfill empty JSON collections in Zones: {ex.Message}");
        }
    }
}
