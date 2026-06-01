using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Data;

namespace OutsourceTracker.BusinessUnit.Divisions;

internal class OrganizationalUnitService : AppDataModelService<OrganizationalUnitDbModel>
{
    public OrganizationalUnitService(IServiceProvider services) : base(services)
    {
    }

    protected override ValueTask OnModelCreated(OrganizationalUnitDbModel model, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        model.CreatedOn = now;
        model.Id = Guid.CreateVersion7(now);
        return base.OnModelCreated(model, cancellationToken);
    }

    /// <summary>
    /// Recalculates TotalAccounts for every Organizational Unit by counting related accounts.
    /// This is a maintenance operation to correct any drift in the denormalized count.
    /// </summary>
    public async Task<int> RecalculateAllAccountCountsAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting recalculation of TotalAccounts for all Organizational Units.");

        // Get accurate counts grouped by OUID (materialize the grouping result)
        var accountCounts = await DataContext.BusinessAccounts
            .AsNoTracking()
            .GroupBy(a => a.OUID)
            .Select(g => new
            {
                OUID = g.Key,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var countLookup = accountCounts.ToDictionary(x => x.OUID, x => x.Count);

        // Load all OUs (tracked so we can update them)
        var allOus = await DataContext.BusinessUnits.ToListAsync(cancellationToken);

        int updated = 0;

        foreach (var ou in allOus)
        {
            int correctCount = countLookup.TryGetValue(ou.Id, out var c) ? c : 0;

            if (ou.TotalAccounts != correctCount)
            {
                ou.TotalAccounts = correctCount;
                updated++;
            }
        }

        if (updated > 0)
        {
            await DataContext.SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Recalculated TotalAccounts. Updated {Updated} organizational units.", updated);
        }
        else
        {
            Logger.LogInformation("Recalculated TotalAccounts. All counts were already correct.");
        }

        return updated;
    }
}
