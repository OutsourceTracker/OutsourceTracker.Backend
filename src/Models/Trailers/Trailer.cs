using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Equipment;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Accounts;

namespace OutsourceTracker.Models.Trailers;

[Index(nameof(FullName), IsUnique = true, AllDescending = true)]
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

    public Guid? LocatedById { get; set; }

    public DateTimeOffset? LocatedDate { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    public Guid? AccountId { get; set; }

    public Account? Account { get; set; }
}
