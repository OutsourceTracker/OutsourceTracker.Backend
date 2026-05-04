using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Authentication;
using OutsourceTracker.BusinessUnit.Accounts;
using OutsourceTracker.Models.Zones;
using System.ComponentModel.DataAnnotations.Schema;

namespace OutsourceTracker.Equipment.Trailers;

public class TrailerDbModel : TrailerModel
{
    [ForeignKey(nameof(AccountId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public AccountDbModel? Account { get; set; }

    [ForeignKey(nameof(ZoneId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public Zone? Zone { get; set; }

    [ForeignKey(nameof(LocatedById))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public ApplicationUser? LocatedBy { get; set; }
}
