using OutsourceTracker.BusinessUnit.Accounts;

namespace OutsourceTracker.BusinessUnit.Divisions;

public class OrganizationalUnitDbModel : OrganizationalUnit
{
    public ICollection<AccountDbModel> Accounts { get; set; }
}
