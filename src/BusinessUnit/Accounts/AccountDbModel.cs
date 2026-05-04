using Microsoft.EntityFrameworkCore;
using OutsourceTracker.BusinessUnit.Divisions;
using System.ComponentModel.DataAnnotations.Schema;

namespace OutsourceTracker.BusinessUnit.Accounts;

public class AccountDbModel : OrganizationalAccount
{
    [ForeignKey(nameof(OUID))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public OrganizationalUnitDbModel OrganizationalUnit { get; set; }
}
