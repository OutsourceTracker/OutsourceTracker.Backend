using OutsourceTracker.Data;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Zones;

namespace OutsourceTracker.Services.ModelService;

internal class ZoneDataService : DynamicDataService<Zone>
{
    public ZoneDataService(IServiceProvider services) : base(services)
    {
    }

    protected override Task NormalizeModel(Zone model, CancellationToken cancellationToken)
    {
        model.ShortCode = model.ShortCode.ToUpperInvariant().Trim();
        model.FullName = model.FullName.Trim();
        return Task.CompletedTask;
    }

    protected override Task OnModelCreated(Zone model, CancellationToken cancellationToken)
    {
        model.DockPoints ??= new List<Vector2>();
        model.EntryPoints ??= new List<Vector2>();
        model.ExitPoints ??= new List<Vector2>();
        return base.OnModelCreated(model, cancellationToken);
    }
}
