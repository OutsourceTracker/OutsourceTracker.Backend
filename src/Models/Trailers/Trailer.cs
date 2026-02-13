using OutsourceTracker.Equipment;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Accounts;

namespace OutsourceTracker.Models.Trailers;

public class Trailer : ITrailer<Guid>
{
    public Guid Id { get; set; }

    public string Prefix { get; set; }

    public string FullName { get; set; }

    public TrailerType Type { get; set; }

    public string Name { get; set; }

    public EquipmentState State { get; set; }

    public MapCoordinates? Location { get; set; }

    public string? LocatedBy { get; set; }

    public DateTimeOffset? LocatedAt { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    public Guid? AccountId { get; set; }

    public Account? Account { get; set; }
}
