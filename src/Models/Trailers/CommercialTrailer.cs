namespace OutsourceTracker.Models.Trailers;

public class CommercialTrailer : ICommericalTrailer<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string FullName { get; set; }

    public string Prefix { get; set; }

    public double? SpottedLatitude { get; set; }

    public double? SpottedLongitude { get; set; }

    public double? SpottedAccuracy { get; set; }

    public string? SpottedBy { get; set; }

    public DateTimeOffset? SpottedOn { get; set; }

    public CommercialTrailer()
    {
        Id = Guid.CreateVersion7();
        Name = Id.ToString().Substring(0, 6).ToUpperInvariant();
        Prefix = "JBHZ";
        FullName = Prefix + Name;
    }

    public override string ToString() => FullName;

    public override int GetHashCode() => HashCode.Combine(Id, Prefix, Name);

    public override bool Equals(object? obj) => obj is CommercialTrailer other && other.Id == Id;
}
