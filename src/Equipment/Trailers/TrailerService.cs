using OutsourceTracker.Data;

namespace OutsourceTracker.Equipment.Trailers;

internal class TrailerService : AppDataModelService<TrailerDbModel>
{
    public TrailerService(IServiceProvider services) : base(services)
    {
    }

    protected override ValueTask OnModelCreated(TrailerDbModel model, CancellationToken cancellationToken = default)
    {
        model.Prefix = model.Prefix.Trim().ToUpperInvariant();
        model.Name = model.Name.Trim().ToUpperInvariant();
        model.FullName = $"{model.Prefix} {model.Name}";
        model.CreatedOn = DateTimeOffset.UtcNow;
        model.Id = Guid.CreateVersion7(model.CreatedOn);
        return base.OnModelCreated(model, cancellationToken);
    }
}
