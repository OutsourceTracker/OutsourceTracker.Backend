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
}
