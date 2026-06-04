using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Data;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Zones;

namespace OutsourceTracker.Services.ModelService;

internal class ZoneDataService : DynamicDataService<Zone>
{
    public ZoneDataService(IServiceProvider services) : base(services)
    {
    }

    /// <summary>
    /// Looks up a zone by its ShortCode (case-insensitive).
    /// Useful for the boundary geometry endpoint which keys off ShortCode.
    /// </summary>
    public async Task<Zone?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shortCode)) return null;
        var normalized = shortCode.ToUpperInvariant().Trim();

        // Use the underlying table for efficient lookup
        return await SelectedTable
            .AsNoTracking()
            .FirstOrDefaultAsync(z => z.ShortCode == normalized, cancellationToken);
    }

    /// <summary>
    /// Finds the first zone whose polygonal boundary contains the given coordinates (using Vector2 X=lat, Y=lng).
    /// Returns null if no zone matches. This is used during trailer "spot" (location update) to
    /// automatically populate denormalized ZoneId/ZoneName on the equipment.
    /// Mirrors the logic from ZoneController.IsInZone but as an injectable service method.
    /// </summary>
    public async Task<Zone?> FindZoneForLocationAsync(Vector2 coordinates, CancellationToken cancellationToken = default)
    {
        if (coordinates == Vector2.Zero)
            return null;

        await foreach (var zone in Search(cancellationToken: cancellationToken).WithCancellation(cancellationToken))
        {
            if (zone.Boundry != null && zone.Boundry.Contains(coordinates))
            {
                return zone;
            }
        }

        return null;
    }

    protected override Task NormalizeModel(Zone model, CancellationToken cancellationToken)
    {
        model.ShortCode = model.ShortCode?.ToUpperInvariant().Trim() ?? string.Empty;
        model.FullName = model.FullName?.Trim() ?? string.Empty;

        // Ensure collections are never null before persisting (value converters + NOT NULL columns in schema)
        model.EntryPoints ??= new List<Vector2>();
        model.ExitPoints ??= new List<Vector2>();
        model.DockPoints ??= new List<Vector2>();
        model.TrailerPools ??= new List<Vector2>();

        // Ensure Boundry is a valid (possibly empty) polygon so the binary converter doesn't see unexpected state
        if (model.Boundry.Points.IsEmpty)
        {
            model.Boundry = new Polygon(Array.Empty<Vector2>());
        }

        return Task.CompletedTask;
    }

    protected override Task OnModelCreated(Zone model, CancellationToken cancellationToken)
    {
        // Defensive: still ensure after create in case other paths bypass Normalize
        model.DockPoints ??= new List<Vector2>();
        model.EntryPoints ??= new List<Vector2>();
        model.ExitPoints ??= new List<Vector2>();
        model.TrailerPools ??= new List<Vector2>();
        return base.OnModelCreated(model, cancellationToken);
    }
}
