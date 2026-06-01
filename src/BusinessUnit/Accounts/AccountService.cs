using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Data;
using OutsourceTracker.Services.DataModels;

namespace OutsourceTracker.BusinessUnit.Accounts;

internal class AccountService : AppDataModelService<AccountDbModel>
{
    public AccountService(IServiceProvider services) : base(services)
    {
    }

    protected override async ValueTask OnModelCreated(AccountDbModel model, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        model.CreatedOn = now;
        model.Id = Guid.CreateVersion7(now);

        // Maintain denormalized TotalAccounts on the parent OU
        if (model.OUID != Guid.Empty)
        {
            var ou = await DataContext.BusinessUnits.FindAsync([model.OUID], cancellationToken);
            if (ou != null)
            {
                ou.TotalAccounts++;
            }
        }

        await base.OnModelCreated(model, cancellationToken);
    }

    public override async Task<ModelResult> Delete(Guid modelId, CancellationToken cancellationToken = default)
    {
        // Capture the OUID before deletion so we can decrement the count
        Guid? ouId = null;
        var account = await DataContext.BusinessAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == modelId, cancellationToken);
        if (account != null)
        {
            ouId = account.OUID;
        }

        var result = await base.Delete(modelId, cancellationToken);

        if (result.Success && ouId.HasValue && ouId.Value != Guid.Empty)
        {
            var ou = await DataContext.BusinessUnits.FindAsync([ouId.Value], cancellationToken);
            if (ou != null && ou.TotalAccounts > 0)
            {
                ou.TotalAccounts--;
            }
        }

        return result;
    }

    public override async Task<ModelResult> Update<T>(Guid modelId, T modelParameters, CancellationToken cancellationToken = default)
    {
        // Load current state to detect OUID change (account move between OUs)
        Guid? oldOuid = null;
        var current = await DataContext.BusinessAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == modelId, cancellationToken);
        if (current != null)
        {
            oldOuid = current.OUID;
        }

        var result = await base.Update(modelId, modelParameters, cancellationToken);

        if (result.Success && result.Data is AccountDbModel updated)
        {
            Guid? newOuid = updated.OUID;

            if (oldOuid != newOuid)
            {
                // Moved out of old OU
                if (oldOuid.HasValue && oldOuid.Value != Guid.Empty)
                {
                    var oldOu = await DataContext.BusinessUnits.FindAsync([oldOuid.Value], cancellationToken);
                    if (oldOu != null && oldOu.TotalAccounts > 0)
                    {
                        oldOu.TotalAccounts--;
                    }
                }

                // Moved into new OU
                if (newOuid.HasValue && newOuid.Value != Guid.Empty)
                {
                    var newOu = await DataContext.BusinessUnits.FindAsync([newOuid.Value], cancellationToken);
                    if (newOu != null)
                    {
                        newOu.TotalAccounts++;
                    }
                }
            }
        }

        return result;
    }
}
