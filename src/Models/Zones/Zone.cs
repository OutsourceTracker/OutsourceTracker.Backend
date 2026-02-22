using OutsourceTracker.Geolocation;

namespace OutsourceTracker.Models.Zones;

public class Zone : IZone<Guid>
{
    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public Polygon Boundry { get; init; }
}
