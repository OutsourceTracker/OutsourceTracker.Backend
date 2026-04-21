using OutsourceTracker.Models.Accounts;
using OutsourceTracker.Models.Zones;

namespace OutsourceTracker.Equipment.Trailers;

public class TrailerDbModel : TrailerModel
{
    public Account? Account { get; set; }

    public Zone? Zone { get; set; }
}
