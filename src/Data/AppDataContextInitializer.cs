
using Microsoft.EntityFrameworkCore;

namespace OutsourceTracker.Data;

public class AppDataContextInitializer : IHostedService
{
    private IServiceProvider _services;

    public AppDataContextInitializer(IServiceProvider services)
    {
        _services = services;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDataContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
