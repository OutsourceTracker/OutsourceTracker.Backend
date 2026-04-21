using OutsourceTracker.Equipment.Trailers;

namespace OutsourceTracker.Services.ModelService;

internal sealed class TrailerDataService : EquipmentDataService<TrailerDbModel>
{
    public TrailerDataService(IServiceProvider services) : base(services)
    {
    }

    protected override Task NormalizeModel(TrailerDbModel model, CancellationToken cancellationToken)
    {
        model.Prefix = model.Prefix.ToUpperInvariant();
        model.Name = model.Name.ToUpperInvariant();
        model.FullName = model.Prefix + model.Name;
        return base.NormalizeModel(model, cancellationToken);
    }

    protected override Task OnModelCreated(TrailerDbModel model, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        model.Prefix ??= "NEW";
        
        if (model.Type == TrailerType.Unknown)
        {
            model.Type = TrailerType.Van;
        }

        return base.OnModelCreated(model, cancellationToken);
    }
}
