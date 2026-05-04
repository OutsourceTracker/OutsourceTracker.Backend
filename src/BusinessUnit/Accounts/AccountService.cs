using OutsourceTracker.Data;

namespace OutsourceTracker.BusinessUnit.Accounts;

internal class AccountService : AppDataModelService<AccountDbModel>
{
    public AccountService(IServiceProvider services) : base(services)
    {
    }

    protected override ValueTask OnModelCreated(AccountDbModel model, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        model.CreatedOn = now;
        model.Id = Guid.CreateVersion7(now);
        return base.OnModelCreated(model, cancellationToken);
    }
}
