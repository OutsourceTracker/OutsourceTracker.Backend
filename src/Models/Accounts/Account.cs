using OutsourceTracker.Accounts;

namespace OutsourceTracker.Models.Accounts;

public class Account : IAccount<Guid>
{
    public Guid Id { get; set; }

    public string ShortName { get; set; }

    public string FullName { get; set; }

    public DateTimeOffset CreatedOn { get; set; }
}
