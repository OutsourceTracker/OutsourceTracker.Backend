
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
}
