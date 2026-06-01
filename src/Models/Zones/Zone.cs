using OutsourceTracker.Geolocation;

namespace OutsourceTracker.Models.Zones;

public class Zone : IZone<Guid>
{
    public Guid Id { get; set; }

    public string ShortCode { get; set; }

    public string FullName { get; set; }

    public Polygon Boundry { get; set; }

    public ICollection<Vector2> EntryPoints { get; set; }

    public ICollection<Vector2> ExitPoints { get; set; }

    public ICollection<Vector2> DockPoints { get; set; }

    /// <summary>
    /// Preferred trailer spotting / parking locations inside this zone.
    /// </summary>
    public ICollection<Vector2> TrailerPools { get; set; }

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

    public bool Equals(Guid other) => Id.Equals(other);
}
